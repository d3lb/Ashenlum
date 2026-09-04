using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Pillars of light fall across the room with gaps left in them. Stand in a gap.
public class KingLines : KingAttack {
    [Header("References")]
    [SerializeField] private KingArena arena;

    [Header("Pattern")]
    [SerializeField] private int slots = 7;
    [SerializeField] private int gaps = 2;

    // Columns between the player and the guaranteed gap. 0 means standing still works.
    [SerializeField] private int gapDistance = 2;

    [Header("Shape")]
    [SerializeField] private Vector2 lineSize = new Vector2(1.2f, 20f);

    [Header("Timing")]
    [SerializeField] private float telegraphTime = 0.9f;
    [SerializeField] private float activeTime = 0.35f;

    private readonly List<int> free = new();

    protected override void Awake() {
        base.Awake();

        if (arena == null) arena = FindFirstObjectByType<KingArena>();

        if (arena == null)
            Debug.LogError($"[KingLines] '{name}' found no KingArena in the scene.", this);
    }

    public override IEnumerator Act(Transform player) {
        if (arena == null) yield break;

        int columns = Mathf.Max(2, slots);
        int holes = Mathf.Clamp(gaps, 1, columns - 1);

        PickGaps(columns, holes, player);

        float telegraph = Telegraph(telegraphTime);

        for (int i = 0; i < columns; i++) {
            if (free.Contains(i)) continue;

            Vector2 pos = new Vector2(arena.SlotX(i, columns), arena.Center.y);
            SpawnLight(pos, lineSize, 0f, telegraph, activeTime);
        }

        yield return new WaitForSeconds(telegraph + activeTime);
    }

    // One gap is always reachable, but never underneath.
    private void PickGaps(int columns, int holes, Transform player) {
        free.Clear();

        if (player != null) {
            float t = Mathf.InverseLerp(arena.LeftX, arena.RightX, player.position.x);
            int here = Mathf.Clamp(Mathf.RoundToInt(t * (columns - 1)), 0, columns - 1);

            int step = Mathf.Max(0, gapDistance);
            int side = Random.value < 0.5f ? -1 : 1;

            // Flip inward if that side falls off the edge.
            if (here + side * step < 0 || here + side * step > columns - 1) side = -side;

            free.Add(Mathf.Clamp(here + side * step, 0, columns - 1));
        }

        int guard = 0;
        while (free.Count < holes && guard++ < 100) {
            int pick = Random.Range(0, columns);
            if (!free.Contains(pick)) free.Add(pick);
        }
    }
}
