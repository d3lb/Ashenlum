using UnityEngine;
using System.Collections;

public class BasicEnemyAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Collider2D attackColliderRight;
    [SerializeField] private Collider2D attackColliderLeft;


    [Header("Settings")]
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackWindup = 0.2f;
    [SerializeField] private float attackDuration = 0.1f;

    private float lastAttackTime;
    private bool isAttacking;

    private EnemyState state;
    [SerializeField] private SpriteRenderer sprite;



    private void Awake()
    {
        state = GetComponent<EnemyState>();
        
        // sprite = GetComponent<SpriteRenderer>();

        attackColliderRight.enabled = false;
        attackColliderLeft.enabled = false;

    }

    private void Update()
    {
        if (state.CurrentState != EnemyState.EnemyStateType.Attack)
            return;

        if (isAttacking)
            return;

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            StartCoroutine(DoAttack());
        }

    }
    private IEnumerator DoAttack()
    {
        isAttacking = true;

        Collider2D active = GetActiveCollider();

        // wait before hit
        sprite.color = Color.red;
        yield return new WaitForSeconds(attackWindup);
        sprite.color = Color.white;


        active.enabled = true;


        yield return new WaitForSeconds(attackDuration);

        active.enabled = false;
        isAttacking = false;
    }

    private Collider2D GetActiveCollider()
    {
        return state.IsFacingRight ? attackColliderRight : attackColliderLeft;
    }
}