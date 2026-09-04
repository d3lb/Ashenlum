using System.Collections;
using UnityEngine;

// Under = collider off, cannot hit or be hit. Surfacing is the whole window.
public class BurrowerAI : MonoBehaviour, IRespawnReset {
    [Header("References")]
    [SerializeField] private SpriteRenderer bodySprite;
    [SerializeField] private Collider2D     bodyCollider;   // hurtbox and contact damage in one
    [SerializeField] private GameObject     mound;
    [SerializeField] private CombatZone     combatZone;

    [Header("Detection")]
    [SerializeField] private float detectRange     = 12f;
    [SerializeField] private float surfaceDistance = 1.2f;

    [Header("Movement")]
    [SerializeField] private float tunnelSpeed = 3.5f;
    [SerializeField] private float riseHeight  = 0.9f;
    [SerializeField] private float riseTime    = 0.12f;

    [Header("Rhythm")]
    [Tooltip("The tell before it bursts out. Shorten this and the enemy stops being fair.")]
    [SerializeField] private float surfaceWindup = 0.35f;

    [Tooltip("How long it stays up. This is the player's entire window to land a hit.")]
    [SerializeField] private float exposedTime = 1.4f;

    [SerializeField] private float submergedRest = 0.8f;

    private EnemyState  state;
    private Rigidbody2D rb;
    private Transform   player;

    private float     burrowY;
    private Coroutine hunt;

    private void Awake() {
        state = GetComponent<EnemyState>();
        rb    = GetComponent<Rigidbody2D>();

        // The depth it lives at, taken from wherever the designer dropped it.
        burrowY = transform.position.y;
    }

    private void Start() {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        ResetForRespawn();
    }

    // Without this it comes back stranded above ground with its hitbox live.
    public void ResetForRespawn() {
        if (hunt != null) StopCoroutine(hunt);

        Submerge();
        state.CurrentState = EnemyState.EnemyStateType.Patrol;

        hunt = StartCoroutine(Hunt());
    }

    private void OnDisable() => hunt = null;

    private void Submerge() {
        if (bodySprite   != null) bodySprite.enabled   = false;
        if (bodyCollider != null) bodyCollider.enabled = false;
        if (mound        != null) mound.SetActive(false);

        transform.position = new Vector3(transform.position.x, burrowY, transform.position.z);
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    private IEnumerator Hunt() {
        while (true) {
            // Dormant until something is worth surfacing for.
            while (!PlayerInRange()) yield return null;

            if (mound != null) mound.SetActive(true);
            state.CurrentState = EnemyState.EnemyStateType.Chase;

            // Tunnel until it is underneath the player.
            while (PlayerInRange() &&
                   Mathf.Abs(player.position.x - transform.position.x) > surfaceDistance) {
                float dir = Mathf.Sign(player.position.x - transform.position.x);

                rb.linearVelocity   = new Vector2(dir * tunnelSpeed, 0f);
                state.IsFacingRight = dir > 0f;

                yield return null;
            }

            rb.linearVelocity = Vector2.zero;

            // Lost them on the way over - go back to sleep rather than surface at nothing.
            if (!PlayerInRange()) {
                Submerge();
                continue;
            }

            // The tell: the mound stops dead before it comes out.
            yield return new WaitForSeconds(surfaceWindup);

            yield return Surface();
            yield return new WaitForSeconds(exposedTime);
            yield return Dive();

            yield return new WaitForSeconds(submergedRest);
        }
    }

    private IEnumerator Surface() {
        state.CurrentState = EnemyState.EnemyStateType.Attack;

        if (mound        != null) mound.SetActive(false);
        if (bodySprite   != null) bodySprite.enabled   = true;
        if (bodyCollider != null) bodyCollider.enabled = true;

        yield return MoveY(burrowY, burrowY + riseHeight, riseTime);
    }

    private IEnumerator Dive() {
        yield return MoveY(transform.position.y, burrowY, riseTime);

        Submerge();
        state.CurrentState = EnemyState.EnemyStateType.Patrol;
    }

    private IEnumerator MoveY(float from, float to, float duration) {
        float t = 0f;

        while (t < duration) {
            t += Time.deltaTime;
            float y = Mathf.Lerp(from, to, t / duration);

            transform.position = new Vector3(transform.position.x, y, transform.position.z);
            yield return null;
        }

        transform.position = new Vector3(transform.position.x, to, transform.position.z);
    }

    private bool PlayerInRange() {
        if (player == null) return false;
        if (Vector2.Distance(transform.position, player.position) > detectRange) return false;

        return IsInsideCombatArea(player.position.x);
    }

    private bool IsInsideCombatArea(float x) {
        if (combatZone == null) return true;

        float minX = Mathf.Min(combatZone.pointA.position.x, combatZone.pointB.position.x);
        float maxX = Mathf.Max(combatZone.pointA.position.x, combatZone.pointB.position.x);

        return x >= minX && x <= maxX;
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, surfaceDistance);
    }
}
