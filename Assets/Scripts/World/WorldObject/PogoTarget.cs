using UnityEngine;

public class PogoTarget : MonoBehaviour, IDamageable {
    [Header("Slash pogo")]
    // A multiplier, so retuning the player rescales every pad.
    [SerializeField] private float pogoMultiplier = 3f;

    public float PogoMultiplier => pogoMultiplier;

    // Hitting the pad with your body instead of your attack flings you and wrecks the jump.
    [Header("Touch bounce")]
    [SerializeField] private float touchBounceForce = 8f;
    [SerializeField] private float touchSideForce = 6f;
    [SerializeField] private float bounceCooldown = 0.15f;
    [SerializeField] private int defaultSideDir = 1;

    private float nextBounceTime;

    private void OnCollisionEnter2D(Collision2D collision) {
        BouncePlayer(collision);
    }

    private void OnCollisionStay2D(Collision2D collision) {
        BouncePlayer(collision);
    }
    public bool TakeDamage(int damage, Vector2 attackerPos) {
        return true;
    }
    private void BouncePlayer(Collision2D collision) {
        if (Time.time < nextBounceTime) return;
        if (!collision.collider.CompareTag("Player")) return;

        Rigidbody2D playerRb = collision.collider.attachedRigidbody;
        if (playerRb == null) return;

        float xDir = playerRb.transform.position.x > transform.position.x ? 1f : -1f;

        if (Mathf.Abs(playerRb.transform.position.x - transform.position.x) < 0.05f)
            xDir = defaultSideDir;

        playerRb.linearVelocity = new Vector2(xDir * touchSideForce, touchBounceForce);
        nextBounceTime = Time.time + bounceCooldown;
    }
}