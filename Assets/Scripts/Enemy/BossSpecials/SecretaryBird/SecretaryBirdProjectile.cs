using UnityEngine;

/// <summary>
/// Straight-flying feather. Damage is handled by your existing EnemyHitbox component
/// on the same prefab - this only moves it and cleans it up.
/// </summary>
public class SecretaryBirdProjectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private float gravity = 0f;
    [SerializeField] private LayerMask despawnLayers;
    [SerializeField] private GameObject impactEffect;

    private Vector2 velocity;

    public void Launch(Vector2 direction, float speed)
    {
        velocity = direction.normalized * speed;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        velocity += Vector2.down * gravity * Time.deltaTime;
        transform.position += (Vector3)(velocity * Time.deltaTime);

        if (velocity.sqrMagnitude > 0.001f)
        {
            float a = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, a);
        }
    }

    private void OnTriggerEnter2D(Collider2D other) => TryDespawn(other.gameObject);
    private void OnCollisionEnter2D(Collision2D c)  => TryDespawn(c.gameObject);

    private void TryDespawn(GameObject go)
    {
        if (((1 << go.layer) & despawnLayers) == 0) return;

        if (impactEffect != null)
            Instantiate(impactEffect, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
