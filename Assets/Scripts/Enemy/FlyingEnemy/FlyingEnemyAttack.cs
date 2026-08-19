using UnityEngine;
using System.Collections;

public class FlyingEnemyAttack : MonoBehaviour, IRespawnReset
{
    [Header("References")]
    [SerializeField] private Collider2D attackCollider;
    [SerializeField] private EnemyAnimation enemyAnimation;
    [SerializeField] private FlyingEnemyAI flyingAI;

    [Header("Dive Settings")]
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float windupTime = 0.4f;       // pause before diving
    [SerializeField] private float diveSpeed = 12f;          // how fast it dives down
    [SerializeField] private float diveDuration = 0.6f;      // max time before giving up dive
    [SerializeField] private float attackTriggerHeight = 1f; // how far above player to start dive

    private float lastAttackTime;
    private EnemyState state;
    private Rigidbody2D rb;
    private Transform playerTransform;

    private void Awake()
    {
        state = GetComponent<EnemyState>();
        rb = GetComponent<Rigidbody2D>();

        ResetForRespawn();

        // Find player — same approach you used in your ground enemy
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }

    // Coroutines die with the object, so the hitbox never gets switched back off.
    public void ResetForRespawn()
    {
        attackCollider.enabled = false;
        lastAttackTime = 0f;
    }

    private void Update()
    {
        // Only attack from Chase state, and only when not already attacking
        if (state.CurrentState != EnemyState.EnemyStateType.Chase)
            return;
        if (state.IsAttacking)
            return;
        if (Time.time < lastAttackTime + attackCooldown)
            return;
        if (playerTransform == null)
            return;

        // Only dive if we're positioned above the player
        float heightAbovePlayer = transform.position.y - playerTransform.position.y;
        bool alignedOnX = Mathf.Abs(transform.position.x - playerTransform.position.x) < 1f;

        if (heightAbovePlayer >= attackTriggerHeight && alignedOnX)
        {
            lastAttackTime = Time.time;
            StartCoroutine(DoDive());
        }
    }

    private IEnumerator DoDive()
    {
        state.IsAttacking = true;
        state.CurrentState = EnemyState.EnemyStateType.Attack;

        rb.linearVelocity = Vector2.zero;
        enemyAnimation.TriggerPrepare();
        yield return new WaitForSeconds(windupTime);

        enemyAnimation.TriggerAttack();
        attackCollider.enabled = true;

        float diveTimer = 0f;

        while (diveTimer < diveDuration)
        {
            diveTimer += Time.deltaTime;
            rb.linearVelocity = new Vector2(0f, -diveSpeed);

            if (transform.position.y <= playerTransform.position.y)
            {
                break;
            }

            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        attackCollider.enabled = false;
        state.IsAttacking = false;

        flyingAI.StartRecover();
    }
}