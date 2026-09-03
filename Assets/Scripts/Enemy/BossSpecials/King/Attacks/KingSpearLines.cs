using System.Collections;
using UnityEngine;

// Short shards of light, angled along the line from him to you, appearing where you
// stand. Not a beam reaching out - just the piece of it that would hit you.
//
// Each one re-aims, so standing still eats all of them and one sidestep only dodges
// one. This is phase 3's constant pressure.
public class KingSpearLines : KingAttack
{
    [Header("Pattern")]
    [SerializeField] private int count = 5;
    [SerializeField] private float gap = 0.3f;

    [Header("Shape")]
    // Long on X, thin on Y. X runs along the direction from him to you.
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

            // Locked where the player was when it was placed, like every other
            // telegraph he has. Following during the warning would make it undodgeable.
            SpawnLight(player.position, shardSize, angle, telegraph, activeTime);

            if (i < shots - 1) yield return new WaitForSeconds(gap);
        }

        yield return new WaitForSeconds(telegraph + activeTime);
    }
}
