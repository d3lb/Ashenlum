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
    [Tooltip("Copies placed in the shuffle bag. 2 = shows up twice as often as a 1.")]
    [SerializeField] private int weight = 1;
    [Tooltip("Phase this move unlocks in. Phase 1 = full health.")]
    [SerializeField] private int minPhase = 1;
    [SerializeField] private float minRange = 0f;
    [SerializeField] private float maxRange = 999f;

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

    public int Weight     => Mathf.Max(1, weight);
    public float Recovery => recovery;
    public float Timeout  => timeout;
    public virtual string DisplayName => GetType().Name;

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

    public virtual bool CanUse(float distanceToPlayer, int phase)
        => phase >= minPhase
        && distanceToPlayer >= minRange
        && distanceToPlayer <= maxRange;

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

    /// <summary>
    /// Take a wall perch, optionally after 1-2 feint hops.
    ///
    /// The wall is chosen HERE and only here, and it is always the wall the player is
    /// furthest from - re-evaluated on every single hop. So if the player runs across the
    /// arena mid-feint, the boss follows to the other side. No attack gets a say in this,
    /// which means no attack can accidentally perch on top of the player.
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
            yield return PerchOnFurthestWall(player, Random.Range(hopHeightRange.x, hopHeightRange.y));
            yield return move.Hold(hopPause);
        }

        yield return PerchOnFurthestWall(player, heightT);
    }

    private IEnumerator PerchOnFurthestWall(Transform player, float heightT)
    {
        int side = Arena.FurthestWallFrom(player.position);
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
