using System.Collections;
using UnityEngine;

// Short shards angled from him to you, spawned where you stand. Each one re-aims.
public class KingSpearLines : KingAttack
{
    [Header("Pattern")]
    [SerializeField] private int count = 5;
    [SerializeField] private float gap = 0.3f;

    [Header("Shape")]
    // X runs along the line from him to you.
    [SerializeField] private Vector2 shardSize = new Vector2(5f, 0.7f);

    [Header("Timing")]
    [SerializeField] private float telegraphTime = 0.4f;
    [SerializeField] private float activeTime = 0.2f;

    public override IEnumerator Act(Transform player)
    {
        if (player == null) yield break;

        int shots = Mathf.Max(1, count);
        float telegraph = Telegraph(telegraphTime);

        for (int i = 0; i < shots; i++)
        {
            Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
            float angle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

            // Locked on placement; following during the warning would be undodgeable.
            SpawnLight(player.position, shardSize, angle, telegraph, activeTime);

            if (i < shots - 1) yield return new WaitForSeconds(gap);
        }

        yield return new WaitForSeconds(telegraph + activeTime);
    }
}
