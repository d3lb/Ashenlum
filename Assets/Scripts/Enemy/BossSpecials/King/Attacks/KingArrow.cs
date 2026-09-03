using System.Collections;
using UnityEngine;

// Four arrows of light form in the top corners, two a side, all aimed at where you are
// standing. Then they all fire at once.
//
// Coming from both corners means there is no direction that is simply "away" - you have
// to leave the point they are converging on, not just back off.
public class KingArrow : KingAttack
{
    [Header("References")]
    [SerializeField] private KingArena arena;

    [Header("Spawn corners")]
    [SerializeField] private int perCorner = 2;

    // How far in from the corner the pair sits, and how far apart the two are.
    [SerializeField] private float cornerInset = 3f;
    [SerializeField] private float pairSpacing = 2.5f;
    [SerializeField] private float dropFromCeiling = 1.5f;

    [Header("Shape")]
    [SerializeField] private Vector2 arrowSize = new Vector2(3f, 0.9f);

    [Header("Timing")]
    // Long on purpose. Four converging arrows need to be readable well before they move.
    [SerializeField] private float aimTime = 0.9f;
    [SerializeField] private float flightTime = 1.2f;

    // Peak speed, not average. The curve below scales it, so the arrow covers less
    // ground than speed x flightTime would suggest.
    [SerializeField] private float speed = 40f;

    // Speed across the flight, 0 to 1. It creeps out of the corner and then snaps,
    // which reads as a throw rather than a constant slide. Drag the first key below
    // zero and it pulls back before firing.
    [SerializeField] private AnimationCurve speedCurve = new AnimationCurve(
        new Keyframe(0f, 0.10f),
        new Keyframe(0.40f, 0.22f),
        new Keyframe(0.65f, 0.85f),
        new Keyframe(1f, 1f));

    // They swivel to follow you for the whole aim, then commit this long before firing.
    // Zero would mean the only possible dodge is after launch; this leaves a beat where
    // the final line is visible and standing there is your choice.
    [SerializeField] private float lockLead = 0.18f;

    protected override void Awake()
    {
        base.Awake();

        if (arena == null) arena = FindFirstObjectByType<KingArena>();

        if (arena == null)
            Debug.LogError($"[KingArrow] '{name}' found no KingArena.", this);
    }

    public override IEnumerator Act(Transform player)
    {
        if (arena == null || player == null) yield break;

        float aim = Telegraph(aimTime);
        float y = arena.CeilY - dropFromCeiling;

        int per = Mathf.Max(1, perCorner);
        var arrows = new KingLight[per * 2];
        var dirs = new Vector2[per * 2];

        int n = 0;
        for (int side = -1; side <= 1; side += 2)
        {
            float baseX = side < 0 ? arena.LeftX + cornerInset : arena.RightX - cornerInset;

            for (int i = 0; i < per; i++)
            {
                Vector2 start = new Vector2(baseX + side * i * pairSpacing, y);
                arrows[n] = SpawnLight(start, arrowSize, 0f, aim, flightTime);
                n++;
            }
        }

        // They track you for the whole aim and only commit at the end, so standing
        // still is lethal and the dodge is a late one rather than a guess.
        float lockAt = Mathf.Max(0f, aim - lockLead);
        float t = 0f;

        while (t < aim)
        {
            t += Time.deltaTime;

            if (t < lockAt && player != null)
            {
                for (int i = 0; i < arrows.Length; i++)
                {
                    if (arrows[i] == null) continue;

                    Vector2 d = ((Vector2)player.position -
                                 (Vector2)arrows[i].transform.position).normalized;

                    dirs[i] = d;
                    arrows[i].transform.rotation =
                        Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                }
            }

            yield return null;
        }

        float flight = 0f;
        while (flight < flightTime)
        {
            flight += Time.deltaTime;

            float v = speed * speedCurve.Evaluate(flightTime <= 0f ? 1f : flight / flightTime);

            for (int i = 0; i < arrows.Length; i++)
                if (arrows[i] != null)
                    arrows[i].transform.position += (Vector3)(dirs[i] * v * Time.deltaTime);

            yield return null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (arena == null) arena = FindFirstObjectByType<KingArena>();
        if (arena == null) return;

        Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.9f);
        float y = arena.CeilY - dropFromCeiling;
        int per = Mathf.Max(1, perCorner);

        for (int side = -1; side <= 1; side += 2)
        {
            float baseX = side < 0 ? arena.LeftX + cornerInset : arena.RightX - cornerInset;

            for (int i = 0; i < per; i++)
                Gizmos.DrawWireCube(new Vector3(baseX + side * i * pairSpacing, y, 0f),
                                    new Vector3(arrowSize.x, arrowSize.y, 0f));
        }
    }
}
