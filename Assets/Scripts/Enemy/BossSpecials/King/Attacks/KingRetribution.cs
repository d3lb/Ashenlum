using System.Collections;
using UnityEngine;

// The answer to standing on top of him and mashing. Circles bloom around his body and
// go off, so burst damage at point blank costs you.
//
// Fired by the brain when it counts enough hits inside its window, never at random.
public class KingRetribution : KingAttack
{
    public override bool Scripted => true;
    public override bool CanOverlap => false;

    [Header("Pattern")]
    [SerializeField] private int circles = 5;

    // How far out from him they sit. Should cover melee range and no further, or it
    // stops being a punish for being close and becomes a room-wide attack.
    [SerializeField] private float spread = 3f;
    [SerializeField] private float blastRadius = 2.2f;

    [Header("Timing")]
    [SerializeField] private float telegraphTime = 0.45f;
    [SerializeField] private float blastTime = 0.2f;

    // Small stagger so they pop in sequence rather than as one flat wall.
    [SerializeField] private float ripple = 0.06f;

    [Header("Damage")]
    [SerializeField] private int damage = 15;

    public override IEnumerator Act(Transform player)
    {
        int count = Mathf.Max(1, circles);
        float telegraph = Telegraph(telegraphTime);

        for (int i = 0; i < count; i++)
        {
            float angle = i / (float)count * Mathf.PI * 2f;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spread;

            // growTime near zero: this is a blast at a fixed size, not an expanding
            // wave. tickInterval longer than blastTime so it can only land once.
            KingRing.Spawn(Brain, transform, offset, blastRadius,
                           telegraph + i * ripple, 0.05f, blastTime,
                           damage, blastTime + 1f);
        }

        yield return new WaitForSeconds(telegraph + (count - 1) * ripple + blastTime + 0.05f);
    }

    private void OnDrawGizmosSelected()
    {
        int count = Mathf.Max(1, circles);

        Gizmos.color = new Color(1f, 0.35f, 0.1f, 0.8f);

        for (int i = 0; i < count; i++)
        {
            float angle = i / (float)count * Mathf.PI * 2f;
            Vector3 p = transform.position +
                        (Vector3)(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spread);

            Gizmos.DrawWireSphere(p, blastRadius);
        }
    }
}
