using UnityEngine;

public class EnemyHitbox : MonoBehaviour {
    [SerializeField] private int damage = 1;
    [SerializeField] private float hitCooldown = 0.5f;
    [SerializeField] private LayerMask playerLayer;

    private float lastHitTime;

    private Collider2D bodyCollider;

    private ContactFilter2D filter;
    private Collider2D[] results = new Collider2D[5];

    private void Awake() {
        bodyCollider = GetComponent<Collider2D>();

        filter = new ContactFilter2D();
        filter.SetLayerMask(playerLayer);
        filter.useTriggers = true;
    }

    private void Update() {
        if (Time.time < lastHitTime + hitCooldown)
            return;

        int count = bodyCollider.Overlap(filter, results);

        for (int i = 0; i < count; i++) {
            PlayerHealth player =
                results[i].GetComponentInParent<PlayerHealth>();

            if (player != null) {
                lastHitTime = Time.time;

                player.TakeDamage(damage, transform.root.position);
            }
        }
    }
}