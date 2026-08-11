using System.Collections;
using UnityEngine;

/// <summary>
/// One attack = one component on the boss GameObject.
///
/// CONTRACT - an attack may NOT:
///   - set CurrentState to Idle or Recover     (the brain owns the loop)
///   - decide what happens next                (the brain owns selection)
///   - call StartCoroutine                     (use `yield return Something()` so the
///                                              brain's watchdog can kill the chain)
///   - choose which wall to perch on           (MoveToWall owns that, always)
/// </summary>
public abstract class SecretaryBirdAttack : MonoBehaviour
{
    [Header("Selection")]
    [Tooltip("Shown in the debug HUD. Use it to tell two components of the same type apart.")]
    [SerializeField] private string label = "";
    [Tooltip("Copies placed in the shuffle bag. 2 = shows up twice as often as a 1.")]
    [SerializeField] private int weight = 1;
    [Tooltip("Phase this move unlocks in. Phase 1 = full health.")]
    [SerializeField] private int minPhase = 1;

    [Header("Windows")]
    [Tooltip("Punish window handed to the player afterwards. THE difficulty dial.")]
    [SerializeField] private float recovery = 0.6f;
    [Tooltip("Hard cap. The brain aborts and cleans up if the attack runs longer.")]
    [SerializeField] private float timeout = 8f;

    [Header("Reposition")]
    [Tooltip("Flying to a perch is a DASH - same speed, same blink, same anticipation. " +
             "Only the line colour and the dead hitbox say it is not a strike.")]
    [SerializeField] private float repositionSpeed = 52f;
    [SerializeField] private float repositionTelegraph = 0.16f;

    [Header("Reposition feint")]
    [Tooltip("Extra wall-to-wall hops before the real perch. Each arrival LOOKS like the " +
             "commit, so the player cannot count on the first landing being the attack. " +
             "This is the fake-out, expressed as movement rather than as a separate move.")]
    [SerializeField, Range(0, 2)] private int maxExtraRepositions = 2;
    [Tooltip("Rolled per hop, so 2 hops is rarer than 1.")]
    [SerializeField, Range(0f, 1f)] private float extraRepositionChance = 0.45f;
    [Tooltip("Heights the feint hops pick from. The real perch always uses the attack's own height.")]
    [SerializeField] private Vector2 hopHeightRange = new Vector2(0.05f, 0.85f);
    [Tooltip("Beat on each feint perch. Long enough to bait a dodge, short enough to stay scary.")]
    [SerializeField] private float hopPause = 0.1f;

    [Header("Wall choice")]
    [Tooltip("Line that splits the room. BELOW it a crossing travels at player height and " +
             "cannot be dodged, so the wall is picked to move AWAY from the player. ABOVE it " +
             "a crossing passes over their head harmlessly, so he just takes the far wall " +
             "from himself and the feint gets its full range back.")]
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
    protected SecretaryBirdArena Arena => move.Arena;

    /// <summary>Which wall the boss is currently on. +1 right, -1 left.</summary>
    protected int CurrentSide => Arena.SideOf(move.Position.x);

    protected virtual void Awake()
    {
        state     = GetComponent<SecretaryBirdState>();
        move      = GetComponent<SecretaryBirdMovement>();
        hitboxes  = GetComponent<SecretaryBirdAttackController>();
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
            yield return telegraph.Flash(move.Position, target, repositionTelegraph, danger);

        yield return move.Dash(target, repositionSpeed);
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
        int hops = 0;
        for (int i = 0; i < maxExtraRepositions; i++)
        {
            if (Random.value > extraRepositionChance) break;
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

    /// <summary>Static flash from wherever the boss is now to wherever it has committed to go.</summary>
    protected IEnumerator ShowPath(Vector2 to, float duration, bool danger = true)
    {
        state.CurrentState = SecretaryBirdState.BossStateType.Windup;

        if (telegraph == null)
        {
            if (duration > 0f) yield return new WaitForSeconds(duration);
            yield break;
        }

        yield return telegraph.Flash(move.Position, to, duration, danger);
    }
}
