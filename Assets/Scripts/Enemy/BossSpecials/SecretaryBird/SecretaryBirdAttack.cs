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

    protected PhaseTuning Pace => pacing != null ? pacing.For(state.Phase) : fallbackPace;

    protected float Speed(float baseSpeed) => baseSpeed * Pace.speedScale;

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

    // Act must use `yield return Something()`, never StartCoroutine, or the brain's
    // watchdog can only kill half the chain when an attack times out.
    public abstract IEnumerator Act(Transform player);

    protected IEnumerator BlinkTo(Vector2 target, bool danger = false)
    {
        state.CurrentState = SecretaryBirdState.BossStateType.Reposition;
        hitboxes.DisableAllHitboxes();

        if (telegraph != null && repositionTelegraph > 0f)
            yield return telegraph.Flash(move.Position, target,
                                         repositionTelegraph * Pace.telegraphScale, danger);

        yield return move.Dash(target, Speed(repositionSpeed));
    }

    protected bool InLowerHalf => Arena.HeightTOf(move.Position.y) < splitHeight;

    private int TargetWall(Transform player)
        => InLowerHalf ? Arena.FurthestWallFrom(player.position) : -CurrentSide;

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
