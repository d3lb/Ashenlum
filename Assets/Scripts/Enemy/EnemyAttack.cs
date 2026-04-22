using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private int attackDamage = 5;


    private float lastAttackTime;

    private EnemyState state;

    private void Awake()
    {
        state = GetComponent<EnemyState>();
    }

    private void Update()
    {
        // only attack in Attack state
        if (state.CurrentState != EnemyState.EnemyStateType.Attack)
            return;

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            AttackPlayer();
        }
    }

    private void AttackPlayer()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRange,
            playerLayer
        );

        foreach (var hit in hits)
        {
            hit.GetComponent<PlayerHealth>()?.TakeDamage(attackDamage, transform.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}