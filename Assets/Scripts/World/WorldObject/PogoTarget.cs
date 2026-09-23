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

    [Header("Reaction")]
    [SerializeField] private Animator animator;
    [SerializeField] private string hitTrigger = "Hit";
    [SerializeField] private ParticleSystem hitParticles;

    private float nextBounceTime;

    // Fired by both a slash and a body bounce, so the pad never sits still when it launches you.
    private void React() {
        if (animator != null) animator.SetTrigger(hitTrigger);
        if (hitParticles != null) hitParticles.Play();
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        BouncePlayer(collision);
    }

    private void OnCollisionStay2D(Collision2D collision) {
        BouncePlayer(collision);
    }
    public bool TakeDamage(int damage, Vector2 attackerPos) {
        React();
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

        // After the cooldown stamp, so OnCollisionStay cannot restart it every frame.
        React();
    }
}