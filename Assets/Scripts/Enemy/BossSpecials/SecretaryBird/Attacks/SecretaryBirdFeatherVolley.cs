using System.Collections;
using UnityEngine;

/// <summary>
/// The pace-breaker. The boss STAYS on the wall and throws a fan of feathers.
///
/// Every other move closes distance; this one refuses to. It forces a positional problem
/// and makes the player walk into range on their own terms, which resets the fight's
/// rhythm and stops the dash loop becoming hypnotic.
///
/// One projectile in the fan is deliberately omitted so a safe slot always exists.
/// Unavoidable spreads are noise; a spread with a findable gap is a skill test.
/// </summary>
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
    [SerializeField] private float featherSpeed = 16f;

    [Header("Fairness")]
    [Tooltip("Skip one feather so a guaranteed safe slot always exists. Keep this ON.")]
    [SerializeField] private bool guaranteeGap = true;

    [Header("Repeat")]
    [SerializeField, Range(1, 3)] private int volleys = 1;
    [SerializeField] private float betweenVolleys = 0.3f;

    public override string DisplayName => volleys > 1 ? $"Feather Volley x{volleys}" : "Feather Volley";

    public override IEnumerator Act(Transform player)
    {
        yield return MoveToWall(player, perchHeight);
        state.FaceTowards(player.position.x);

        Vector2 aim = player.position;
        yield return ShowPath(aim, fanTime);

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
            SecretaryBirdProjectile p = go.GetComponent<SecretaryBirdProjectile>();
            if (p != null) p.Launch(dir, featherSpeed);
        }
    }
}
