using System.Collections;
using UnityEngine;


public abstract class SecretaryBirdAttack : MonoBehaviour
{
    [Header("Selection")]
    [SerializeField] private string label = "";
    [SerializeField] private int weight = 1;
    [SerializeField] private int minPhase = 1;

    [Header("Windows")]
    [SerializeField] private float recovery = 0.6f;
    [SerializeField] private float timeout = 8f;

    [Header("Reposition")]
    [SerializeField] private float repositionSpeed = 52f;
    [SerializeField] private float repositionTelegraph = 0.16f;

    [Header("Reposition feint")]

    [SerializeField] private Vector2 hopHeightRange = new Vector2(0.05f, 0.85f);
    [SerializeField] private float hopPause = 0.1f;

    [Header("Wall choice")]
    [SerializeField, Range(0f, 1f)] private float splitHeight = 0.5f;

    public int Weight     => Mathf.Max(1, weight);
    public float Recovery => recovery;
    public float Timeout  => timeout;
    public virtual string DisplayName =>
        string.IsNullOrWhiteSpace(label) ? GetType().Name : label;

    protected SecretaryBirdState state;
    protected SecretaryBirdMovement move;
    protected SecretaryBirdAttackController hitboxes;
    protected SecretaryBirdTelegraph telegraph;
    protected SecretaryBirdPacing pacing;
    protected SecretaryBirdArena Arena => move.Arena;

    private static readonly PhaseTuning fallbackPace = new PhaseTuning();

    /// <summary>Tuning for the phase the fight is currently in.</summary>
    protected PhaseTuning Pace => pacing != null ? pacing.For(state.Phase) : fallbackPace;

    /// <summary>Apply the phase's speed scaling. Every dash in every attack goes through this.</summary>
    protected float Speed(float baseSpeed) => baseSpeed * Pace.speedScale;

    /// <summary>Which wall the boss is currently on. +1 right, -1 left.</summary>
    protected int CurrentSide => Arena.SideOf(move.Position.x);

    protected virtual void Awake()
    {
        state     = GetComponent<SecretaryBirdState>();
        move      = GetComponent<SecretaryBirdMovement>();
        hitboxes  = GetComponent<SecretaryBirdAttackController>();
        pacing    = GetComponent<SecretaryBirdPacing>();
        telegraph = GetComponentInChildren<SecretaryBirdTelegraph>(true);
    }

    public virtual bool CanUse(int phase) => phase >= minPhase;

    public abstract IEnumerator Act(Transform player);

    /// <summary>Telegraphed, non-damaging blink. Safe-coloured line by default.</summary>
    protected IEnumerator BlinkTo(Vector2 target, bool danger = false)
    {
        state.CurrentState = SecretaryBirdState.BossStateType.Reposition;
        hitboxes.DisableAllHitboxes();

        if (telegraph != null && repositionTelegraph > 0f)
            yield return telegraph.Flash(move.Position, target,
                                         repositionTelegraph * Pace.telegraphScale, danger);

        yield return move.Dash(target, Speed(repositionSpeed));
    }

    /// <summary>Below the split line, a horizontal crossing travels through player space.</summary>
    protected bool InLowerHalf => Arena.HeightTOf(move.Position.y) < splitHeight;

    /// <summary>
    /// Low  -> the wall furthest from the PLAYER, so the blink moves away from them.
    /// High -> the wall furthest from the BOSS, so every hop is a real crossing.
    /// </summary>
    private int TargetWall(Transform player)
        => InLowerHalf ? Arena.FurthestWallFrom(player.position) : -CurrentSide;

    /// <summary>
    /// Take a wall perch, optionally after 1-2 feint hops.
    ///
    /// Wall choice is never the attack's business - it is decided here, by altitude.
    /// </summary>
    protected IEnumerator MoveToWall(Transform player, float heightT)
    {
        // Feint count comes from the phase, not the attack. Phase 1 has none - the honest
        // move has to be learned before a lie about it can mean anything.
        int hops = 0;
        for (int i = 0; i < Pace.maxFeints; i++)
        {
            if (Random.value > Pace.feintChance) break;
            hops++;
        }

        for (int i = 0; i < hops; i++)
        {
            yield return FeintHop(player);
            yield return move.Hold(hopPause);
        }

        yield return PerchOn(TargetWall(player), heightT);
    }

    private IEnumerator FeintHop(Transform player)
    {
        int side = TargetWall(player);
        float height = RandomHopHeight();

        // Low, and the safe wall is the one he is already on. There is no safe horizontal
        // move from here, so he climbs instead of stuttering in place - which also lifts him
        // above the split line, where the next hop becomes a free crossing over the player.
        if (InLowerHalf && side == CurrentSide)
            height = Mathf.Max(height, Random.Range(splitHeight + 0.1f, hopHeightRange.y));

        yield return PerchOn(side, height);
    }

    private float RandomHopHeight() => Random.Range(hopHeightRange.x, hopHeightRange.y);

    private IEnumerator PerchOn(int side, float heightT)
    {
        yield return BlinkTo(Arena.Perch(side, heightT));
        state.SetFacing(side < 0);
    }

    /// <summary>
    /// Static flash from wherever the boss is now to wherever it has committed to go.
    /// Duration is scaled by the phase, so every attack gets longer warning early on
    /// without any attack needing to know phases exist.
    /// </summary>
    protected IEnumerator ShowPath(Vector2 to, float duration, bool danger = true)
    {
        state.CurrentState = SecretaryBirdState.BossStateType.Windup;
        duration *= Pace.telegraphScale;

        if (telegraph == null)
        {
            if (duration > 0f) yield return new WaitForSeconds(duration);
            yield break;
        }

        yield return telegraph.Flash(move.Position, to, duration, danger);
    }
}
