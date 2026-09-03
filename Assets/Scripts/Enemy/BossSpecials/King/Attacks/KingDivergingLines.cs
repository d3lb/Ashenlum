using System.Collections;
using UnityEngine;

// Pillars fall next to him and march outward toward the walls, shoving the player off.
//
// This started life as the opposite - closing in from the walls - on the assumption the
// player would camp the edge. Testing showed the reverse: the centre is where the damage
// is, so standing next to him is what needed punishing, and herding him there made the
// fight easier rather than harder.
public class KingDivergingLines : KingAttack
{
    [Header("References")]
    [SerializeField] private KingArena arena;

    [Header("Pattern")]
    [SerializeField] private int waves = 4;

    // 0 is on him, 1 is at the wall. The first pair starts just off his body and the
    // last stops short, so there is standing room at each wall to be pushed into.
    [Range(0f, 1f)] [SerializeField] private float startFromKing = 0.15f;
    [Range(0f, 1f)] [SerializeField] private float endBeforeWall = 0.8f;

    [Header("Shape")]
    [SerializeField] private Vector2 lineSize = new Vector2(1.2f, 20f);

    [Header("Timing")]
    [SerializeField] private float telegraphTime = 0.7f;
    [SerializeField] private float activeTime = 0.3f;

    // Shorter than telegraph + active, so waves overlap and it reads as one push
    // outward rather than four separate attacks.
    [SerializeField] private float waveGap = 0.45f;

    protected override void Awake()
    {
        base.Awake();

        if (arena == null) arena = FindFirstObjectByType<KingArena>();

        if (arena == null)
            Debug.LogError($"[KingDivergingLines] '{name}' found no KingArena.", this);
    }

    public override IEnumerator Act(Transform player)
    {
        if (arena == null) yield break;

        int count = Mathf.Max(1, waves);
        float telegraph = Telegraph(telegraphTime);
        float centerX = arena.Center.x;
        float y = arena.Center.y;

        for (int i = 0; i < count; i++)
        {
            float t = count == 1
                ? endBeforeWall
                : Mathf.Lerp(startFromKing, endBeforeWall, i / (float)(count - 1));

            SpawnLight(new Vector2(Mathf.Lerp(centerX, arena.LeftX, t), y),
                       lineSize, 0f, telegraph, activeTime);

            SpawnLight(new Vector2(Mathf.Lerp(centerX, arena.RightX, t), y),
                       lineSize, 0f, telegraph, activeTime);

            if (i < count - 1) yield return new WaitForSeconds(waveGap);
        }

        yield return new WaitForSeconds(telegraph + activeTime);
    }

    private void OnDrawGizmosSelected()
    {
        if (arena == null) arena = FindFirstObjectByType<KingArena>();
        if (arena == null) return;

        float centerX = arena.Center.x;
        int count = Mathf.Max(1, waves);

        for (int i = 0; i < count; i++)
        {
            float t = count == 1
                ? endBeforeWall
                : Mathf.Lerp(startFromKing, endBeforeWall, i / (float)(count - 1));

            // Later waves are redder, so the outward order is readable in the editor.
            Gizmos.color = Color.Lerp(Color.yellow, Color.red, i / Mathf.Max(1f, count - 1f));

            foreach (float x in new[] { Mathf.Lerp(centerX, arena.LeftX, t),
                                        Mathf.Lerp(centerX, arena.RightX, t) })
                Gizmos.DrawWireCube(new Vector3(x, arena.Center.y, 0f),
                                    new Vector3(lineSize.x, lineSize.y, 0f));
        }

        // Green marks the standing room left at each wall.
        Gizmos.color = Color.green;
        float lastL = Mathf.Lerp(centerX, arena.LeftX, endBeforeWall);
        float lastR = Mathf.Lerp(centerX, arena.RightX, endBeforeWall);

        Gizmos.DrawLine(new Vector3(arena.LeftX, arena.FloorY), new Vector3(lastL, arena.FloorY));
        Gizmos.DrawLine(new Vector3(lastR, arena.FloorY), new Vector3(arena.RightX, arena.FloorY));
    }
}
