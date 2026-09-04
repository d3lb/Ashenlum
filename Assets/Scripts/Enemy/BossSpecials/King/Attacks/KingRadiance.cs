using System.Collections;
using UnityEngine;

// The phase-change burst. Same move at both transitions, so it is learned once.
public class KingRadiance : KingAttack {
    public override bool Scripted => true;
    public override bool CanOverlap => false;

    [Header("References")]
    [SerializeField] private KingArena arena;

    [Header("Reach")]
    // Under the arena half-width, or there is nowhere to escape to.
    [SerializeField] private float maxRadius = 14f;

    [Header("Timing")]
    [SerializeField] private float chargeTime = 1.2f;
    [SerializeField] private float growTime = 1.4f;
    [SerializeField] private float holdTime = 0.6f;

    [Header("Damage")]
    // Lower than a normal light: being caught means several ticks.
    [SerializeField] private int tickDamage = 10;
    [SerializeField] private float tickInterval = 0.5f;

    protected override void Awake() {
        base.Awake();

        if (arena == null) arena = FindFirstObjectByType<KingArena>();

        if (arena != null && maxRadius >= arena.Width * 0.5f)
            Debug.LogWarning($"[KingRadiance] maxRadius {maxRadius} covers the whole room " +
                             $"(half-width {arena.Width * 0.5f:0.0}). There is nowhere to run.", this);
    }

    public override IEnumerator Act(Transform player) {
        KingRing.Spawn(Brain, transform, Vector2.zero, maxRadius,
                       Telegraph(chargeTime), growTime, holdTime,
                       tickDamage, tickInterval);

        yield return new WaitForSeconds(Telegraph(chargeTime) + growTime + holdTime);
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, maxRadius);

        if (arena == null) arena = FindFirstObjectByType<KingArena>();
        if (arena == null) return;

        // Green is the floor left once it is fully open.
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(arena.LeftX, arena.FloorY), new Vector3(transform.position.x - maxRadius, arena.FloorY));
        Gizmos.DrawLine(new Vector3(transform.position.x + maxRadius, arena.FloorY), new Vector3(arena.RightX, arena.FloorY));
    }
}
