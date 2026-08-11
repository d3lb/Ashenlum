using System.Collections;
using UnityEngine;

public class SecretaryBirdProjectile : MonoBehaviour
{
    [Header("Flight")]
    [SerializeField] private float speed = 16f;
    [SerializeField] private float gravity = 0f;
    [SerializeField] private float lifetime = 4f;

    [Header("Orientation")]
    [SerializeField] private float spriteAngleOffset = 0f;

    [SerializeField] private bool realignInFlight = false;

    [Header("On hitting ground")]
    [SerializeField] private LayerMask groundLayers;
    [Tooltip("How far past the surface it buries itself, so it reads as stuck IN the floor " +
             "rather than balanced on top of it.")]
    [SerializeField] private float embedDepth = 0.15f;
    [SerializeField] private float stuckLifetime = 3f;
    [SerializeField] private float fadeOutTime = 0.5f;

    private Vector2 velocity;
    private bool stuck;
    private float despawnAt;

    public void Launch(Vector2 direction, float speedOverride = -1f)
    {
        velocity = direction.normalized * (speedOverride > 0f ? speedOverride : speed);
        despawnAt = Time.time + lifetime;
        Aim();
    }

    private void Aim()
    {
        if (velocity.sqrMagnitude < 0.0001f) return;
        float a = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg + spriteAngleOffset;
        transform.rotation = Quaternion.Euler(0f, 0f, a);
    }

    private void Update()
    {
        if (stuck) return;

        velocity += Vector2.down * gravity * Time.deltaTime;

        Vector2 from = transform.position;
        Vector2 step = velocity * Time.deltaTime;


        if (step.sqrMagnitude > 0.0000001f)
        {
            RaycastHit2D hit = Physics2D.Raycast(from, step.normalized, step.magnitude, groundLayers);
            if (hit.collider != null)
            {
                transform.position = hit.point + step.normalized * embedDepth;
                Stick();
                return;
            }
        }

        transform.position = from + step;

        if (realignInFlight && gravity != 0f) Aim();

        if (Time.time >= despawnAt) Destroy(gameObject);
    }

    private void Stick()
    {
        stuck = true;
        velocity = Vector2.zero;

        EnemyHitbox hitbox = GetComponent<EnemyHitbox>();
        if (hitbox != null) hitbox.enabled = false;

        StartCoroutine(Embedded());
    }

    private IEnumerator Embedded()
    {
        float hold = Mathf.Max(0f, stuckLifetime - fadeOutTime);
        if (hold > 0f) yield return new WaitForSeconds(hold);

        SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>();
        float t = 0f;

        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(t / fadeOutTime);

            foreach (SpriteRenderer sr in sprites)
            {
                Color c = sr.color;
                c.a = k;
                sr.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
