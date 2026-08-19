using System.Collections;
using UnityEngine;

public class SecretaryBirdFeatherVolley : SecretaryBirdAttack
{
    [Header("Perch")]
    [SerializeField, Range(0f, 1f)] private float perchHeight = 0.7f;

    [Header("Telegraph")]
    [SerializeField] private float fanTime = 0.4f;

    [Header("Volley")]
    [SerializeField] private GameObject featherPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField, Range(2, 9)] private int featherCount = 5;
    [SerializeField] private float spreadAngle = 55f;

    [Header("Fairness")]
    [SerializeField] private bool guaranteeGap = true;

    [Header("Repeat")]
    [SerializeField, Range(1, 3)] private int volleys = 1;
    [SerializeField] private float betweenVolleys = 0.3f;

    public override string DisplayName => volleys > 1 ? $"Feather Volley x{volleys}" : "Feather Volley";

    public override IEnumerator Act(Transform player)
    {
        yield return MoveToWall(player, perchHeight);
        state.FaceTowards(player.position.x);

        // No line: a spread has no single path to promise.
        Vector2 aim = player.position;
        state.CurrentState = SecretaryBirdState.BossStateType.Windup;
        yield return move.Hold(fanTime);

        for (int v = 0; v < volleys; v++)
        {
            state.CurrentState = SecretaryBirdState.BossStateType.Attacking;
            Fire(aim);
            if (v < volleys - 1) yield return move.Hold(betweenVolleys);
        }

        yield return move.Hold(0.12f);
    }

    private void Fire(Vector2 aim)
    {
        if (featherPrefab == null) return;

        Vector2 origin = firePoint != null ? (Vector2)firePoint.position : move.Position;
        Vector2 toAim = (aim - origin).normalized;
        float baseAngle = Mathf.Atan2(toAim.y, toAim.x) * Mathf.Rad2Deg;

        int gap = guaranteeGap ? Random.Range(0, featherCount) : -1;
        float step = featherCount > 1 ? spreadAngle / (featherCount - 1) : 0f;

        for (int i = 0; i < featherCount; i++)
        {
            if (i == gap) continue;

            float angle = baseAngle - spreadAngle * 0.5f + step * i;
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad),
                                      Mathf.Sin(angle * Mathf.Deg2Rad));

            GameObject go = Instantiate(featherPrefab, origin, Quaternion.Euler(0f, 0f, angle));
            // Speed comes from the Feather prefab. One source of truth.
            SecretaryBirdProjectile p = go.GetComponent<SecretaryBirdProjectile>();
            if (p != null) p.Launch(dir);
        }
    }
}
