using System.Collections.Generic;
using UnityEngine;

public class AbilityProjectile : MonoBehaviour
{
    [Header("Flight")]
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private float spriteAngleOffset = 0f;

    // Multiplies the base speed across the shot's life: drifts out, hangs, then snaps.
    [SerializeField] private AnimationCurve speedCurve = new AnimationCurve(
        new Keyframe(0f, 0.8f),
        new Keyframe(0.45f, 0.15f),
        new Keyframe(1f, 2.4f));

    // How long the curve takes to play out. After that it holds its last value.
    [SerializeField] private float curveTime = 0.7f;

    [Header("Hits")]
    // Match this to the visible circle. A zero-width ray from the centre slips past
    // anything the sprite clearly overlapped, which reads as the shot passing through.
    [SerializeField] private float radius = 0.5f;
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private bool pierce = false;
    [SerializeField] private GameObject impactPrefab;

    private readonly HashSet<IDamageable> alreadyHit = new();
    private Vector2 direction;
    private float baseSpeed;
    private float elapsed;
    private int damage;
    private float despawnAt;

    public void Launch(Vector2 dir, float speed, int dmg)
    {
        direction = dir.normalized;
        baseSpeed = speed;
        damage = dmg;
        despawnAt = Time.time + lifetime;

        float a = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + spriteAngleOffset;
        transform.rotation = Quaternion.Euler(0f, 0f, a);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        float k = curveTime <= 0f ? 1f : Mathf.Clamp01(elapsed / curveTime);
        float speed = baseSpeed * speedCurve.Evaluate(k);

        Vector2 from = transform.position;
        Vector2 step = direction * speed * Time.deltaTime;

        if (step.sqrMagnitude > 0.0000001f)
        {
            RaycastHit2D[] hits = Physics2D.CircleCastAll(
                from, radius, step.normalized, step.magnitude, enemyLayers | groundLayers);

            System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

            foreach (RaycastHit2D hit in hits)
            {
                // A cast that starts already overlapping reports distance 0 and a zero
                // point, which would put the impact effect at the world origin.
                Vector2 point = hit.distance > 0f ? hit.point : from;

                if (((1 << hit.collider.gameObject.layer) & groundLayers) != 0)
                {
                    Impact(point);
                    return;
                }

                IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
                if (target == null || !alreadyHit.Add(target)) continue;

                target.TakeDamage(damage, from);

                if (!pierce)
                {
                    Impact(point);
                    return;
                }
            }
        }

        transform.position = from + step;

        if (Time.time >= despawnAt) Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    private void Impact(Vector2 at)
    {
        if (impactPrefab != null) Instantiate(impactPrefab, at, Quaternion.identity);
        Destroy(gameObject);
    }
}
