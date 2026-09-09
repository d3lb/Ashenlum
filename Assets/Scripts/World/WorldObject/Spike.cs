using System.Collections;
using UnityEngine;

// Costs health and a few seconds, not the run. An instant kill here would mean an enemy's
// knockback could take your whole purse for a hit you never chose to take.
public class Spike : MonoBehaviour {
    [SerializeField] private int damageAmount = 20;

    // Hand placed, because "somewhere safe near a spike pit" is a level design question and
    // not one a rule can answer. As many as the pit needs; the nearest one wins.
    [Header("Put down at")]
    [SerializeField] private Transform[] points;

    // After the move, not before: the beat is for reading where you ended up.
    [Header("Ejection")]
    [SerializeField] private float freezeTime = 0.25f;

    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeAmplitude = 1.5f;
    [SerializeField] private float shakeFrequency = 2f;

    private bool ejecting;

    private void OnTriggerEnter2D(Collider2D collision) => Bite(collision);

    // Enter fires once. Landing in here during hit invincibility would otherwise never be
    // asked again, and the player walks around inside the spikes unharmed.
    private void OnTriggerStay2D(Collider2D collision) => Bite(collision);

    // A frozen game still runs triggers, so without this the coroutine restarts every frame.
    private void OnDisable() {
        if (!ejecting) return;

        ejecting = false;
        TimeManager.Release(this);
    }

    private void Bite(Collider2D collision) {
        if (ejecting || !collision.CompareTag("Player")) return;

        PlayerHealth health = collision.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        // TakeDamage is silent about whether it landed, and LastHitTime is how it says so.
        float before = health.LastHitTime;
        health.TakeDamage(damageAmount, transform.position);

        if (health.LastHitTime == before) return;

        // Dead already: the respawn owns where he ends up.
        if (health.CurrentHP <= 0) return;

        PlayerMovement movement = collision.GetComponentInParent<PlayerMovement>();
        if (movement == null || movement.rb == null) return;

        StartCoroutine(Eject(movement));
    }

    private IEnumerator Eject(PlayerMovement movement) {
        ejecting = true;

        movement.rb.linearVelocity = Vector2.zero;
        movement.rb.position = SafeSpot(movement.LastSafeGround);

        if (CameraShakeManager.Instance != null)
            CameraShakeManager.Instance.Shake(shakeDuration, shakeAmplitude, shakeFrequency);

        TimeManager.Freeze(this);
        yield return new WaitForSecondsRealtime(freezeTime);
        TimeManager.Release(this);

        // Gravity would otherwise carry over whatever built up during the freeze.
        movement.rb.linearVelocity = Vector2.zero;

        ejecting = false;
    }

    // Nearest to where he last stood, so he is put back on the side he came from. With no
    // points he goes back to the lip of the ledge - workable, but the reason they exist.
    // Squared distance: this only ranks them, and the square root would change nothing.
    private Vector2 SafeSpot(Vector2 ground) {
        Vector2 best = ground;
        float bestDistance = float.MaxValue;

        if (points == null) return best;

        foreach (Transform point in points) {
            if (point == null) continue;

            float distance = Vector2.SqrMagnitude((Vector2)point.position - ground);
            if (distance >= bestDistance) continue;

            bestDistance = distance;
            best = point.position;
        }

        return best;
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.green;
        if (points == null) return;

        foreach (Transform point in points) {
            if (point == null) continue;

            Gizmos.DrawWireSphere(point.position, 0.3f);
            Gizmos.DrawLine(transform.position, point.position);
        }
    }
}
