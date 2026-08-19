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
    [SerializeField] private float radius = 0.5f;
    [SerializeField] private LayerMask enemyLayers;

    [Header("Walls")]
    [SerializeField] private LayerMask groundLayers;

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
            RaycastHit2D[] hits = Physics2D.CircleCastAll(from, radius, step.normalized, step.magnitude, enemyLayers);

            System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

            foreach (RaycastHit2D hit in hits)
            {
                Vector2 point = hit.distance > 0f ? hit.point : from;

                IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
                if (target == null || !alreadyHit.Add(target)) continue;

                target.TakeDamage(damage, from);
            }

            // A thin ray, not the circle: the radius sticks out above and below the shot,
            // so a circle cast would die on any floor or ceiling it merely passes. The
            // shot only ever travels dead left or right, so this can only meet a wall.
            RaycastHit2D wall = Physics2D.Raycast(from, step.normalized, step.magnitude, groundLayers);

            if (wall.collider != null)
            {
                Destroy(gameObject);
                return;
            }
        }

        transform.position = from + step;

        if (Time.time >= despawnAt) Destroy(gameObject);
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
