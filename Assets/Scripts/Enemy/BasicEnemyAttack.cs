using UnityEngine;
using System.Collections;

public class BasicEnemyAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Collider2D attackColliderRight;
    [SerializeField] private Collider2D attackColliderLeft;
    [SerializeField] private EnemyAnimation enemyAnimation;

    [Header("Settings")]
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackWindup = 0.2f;
    [SerializeField] private float attackDuration = 0.1f;
    [SerializeField] private float lungeForce = 3f;

    private float lastAttackTime;

    private EnemyState state;
    private SpriteRenderer sprite;
    private Rigidbody2D rb;



    private void Awake()
    {
        state = GetComponent<EnemyState>();
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();

        attackColliderRight.enabled = false;
        attackColliderLeft.enabled = false;

    }

    private void Update()
    {
        if (state.CurrentState != EnemyState.EnemyStateType.Attack)
            return;

        if (state.IsAttacking)
            return;

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            StartCoroutine(DoAttack());
        }

    }
    private IEnumerator DoAttack()
    {

        state.IsAttacking = true;

        // wait before hit
        enemyAnimation.TriggerPrepare();
        yield return new WaitForSeconds(attackWindup);
        enemyAnimation.TriggerAttack();

        Collider2D active = GetActiveCollider();

        // Add Lunge force
        float dir = state.IsFacingRight ? 1f : -1f;

        rb.AddForce(
            new Vector2(dir * lungeForce, 0f),
            ForceMode2D.Impulse
        );

        // Handle the collider
        active.enabled = true;

        yield return new WaitForSeconds(attackDuration);

        active.enabled = false;

        state.IsAttacking = false;
    }

    private Collider2D GetActiveCollider()
    {
        return state.IsFacingRight ? attackColliderRight : attackColliderLeft;
    }
}