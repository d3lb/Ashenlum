using System.Collections;
using UnityEngine;

// Light erupts from the floor under the player.
//
// Set count above 1 and it becomes a volley that re-aims between each one, so you have
// to keep moving instead of sidestepping once. That is also how phase 3 gets its
// "spikes in threes" - a second copy of this component with count 3 and minPhase 3.
// The brain finds attacks with GetComponents, so two of these on the King is fine.
public class KingSpike : KingAttack
{
    [Header("References")]
    [SerializeField] private KingArena arena;

    [Header("Pattern")]
    [SerializeField] private int count = 1;

    // Gap between spikes in a volley. Each one re-reads where the player is.
    [SerializeField] private float burstGap = 0.35f;

    [Header("Shape")]
    [SerializeField] private Vector2 spikeSize = new Vector2(1.6f, 3.5f);

    [Header("Timing")]
    [SerializeField] private float telegraphTime = 0.55f;
    [SerializeField] private float activeTime = 0.25f;

    protected override void Awake()
    {
        base.Awake();

        if (arena == null) arena = FindFirstObjectByType<KingArena>();

        if (arena == null)
            Debug.LogError($"[KingSpike] '{name}' found no KingArena.", this);
    }

    public override IEnumerator Act(Transform player)
    {
        if (arena == null || player == null) yield break;

        int shots = Mathf.Max(1, count);
        float telegraph = Telegraph(telegraphTime);

        for (int i = 0; i < shots; i++)
        {
            // Position locks the moment it is placed and does not follow during the
            // telegraph. A spike that tracks you while warning you cannot be dodged.
            float x = arena.ClampX(player.position.x);
            Vector2 pos = new Vector2(x, arena.FloorY + spikeSize.y * 0.5f);

            SpawnLight(pos, spikeSize, 0f, telegraph, activeTime);

            if (i < shots - 1) yield return new WaitForSeconds(burstGap);
        }

        yield return new WaitForSeconds(telegraph + activeTime);
    }

    private void OnDrawGizmosSelected()
    {
        if (arena == null) arena = FindFirstObjectByType<KingArena>();
        if (arena == null) return;

        // Drawn at the King's own x, just to show the size and where the floor is.
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.8f);
        Gizmos.DrawWireCube(new Vector3(transform.position.x,
                                        arena.FloorY + spikeSize.y * 0.5f, 0f),
                            new Vector3(spikeSize.x, spikeSize.y, 0f));
    }
}
