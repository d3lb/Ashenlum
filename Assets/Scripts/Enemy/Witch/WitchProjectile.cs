using UnityEngine;

// The witch's copy of the player's shot. Deliberately a separate script: the player's
// AbilityProjectile finds targets through IDamageable, which the player himself is not,
// and widening that interface to include him makes every attack in the game able to hit him.
public class WitchProjectile : MonoBehaviour {
    [Header("Flight")]
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private float spriteAngleOffset = 0f;

    [SerializeField] private AnimationCurve speedCurve = new AnimationCurve(
        new Keyframe(0f, 0.8f),
        new Keyframe(0.45f, 0.15f),
        new Keyframe(1f, 2.4f));

    [SerializeField] private float curveTime = 0.7f;

    [Header("Hits")]
    [SerializeField] private float radius = 0.4f;
    [SerializeField] private LayerMask playerLayers;

    [Header("Walls")]
    [SerializeField] private LayerMask groundLayers;

    private Vector2 direction;
    private float baseSpeed;
    private float elapsed;
    private int damage;
    private float despawnAt;
    private bool spent;

    public void Launch(Vector2 dir, float speed, int dmg) {
        direction = dir.normalized;
        baseSpeed = speed;
        damage = dmg;
        despawnAt = Time.time + lifetime;

        float a = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + spriteAngleOffset;
        transform.rotation = Quaternion.Euler(0f, 0f, a);
    }

    private void Update() {
        elapsed += Time.deltaTime;

        float k = curveTime <= 0f ? 1f : Mathf.Clamp01(elapsed / curveTime);
        float speed = baseSpeed * speedCurve.Evaluate(k);

        Vector2 from = transform.position;
        Vector2 step = direction * speed * Time.deltaTime;

        if (step.sqrMagnitude > 0.0000001f) {
            if (!spent) {
                RaycastHit2D hit = Physics2D.CircleCast(
                    from, radius, step.normalized, step.magnitude, playerLayers);

                if (hit.collider != null) {
                    PlayerHealth player = hit.collider.GetComponentInParent<PlayerHealth>();

                    if (player != null) {
                        // One hit only, and the shot keeps flying rather than popping on
                        // an i-framed player who took no damage.
                        spent = true;
                        player.TakeDamage(damage, from);
                    }
                }
            }

            // A thin ray, not the circle: the radius sticks out above and below the shot,
            // so a circle cast would die on any floor or ceiling it merely passes.
            RaycastHit2D wall = Physics2D.Raycast(from, step.normalized, step.magnitude, groundLayers);

            if (wall.collider != null) {
                Destroy(gameObject);
                return;
            }
        }

        transform.position = from + step;

        if (Time.time >= despawnAt) Destroy(gameObject);
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
