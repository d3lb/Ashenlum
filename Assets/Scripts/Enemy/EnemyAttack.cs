using UnityEngine;
using System.Collections;

public class EnemyAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Collider2D attackColliderRight;
    [SerializeField] private Collider2D attackColliderLeft;
    [SerializeField] private LayerMask playerLayer;

    [Header("Settings")]
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackWindup = 0.2f;
    [SerializeField] private float attackDuration = 0.1f;
    [SerializeField] private int damage = 5;

    private float lastAttackTime;

    private EnemyState state;
    private ContactFilter2D filter;
    private Collider2D[] results = new Collider2D[5];

    private void Awake()
    {
        state = GetComponent<EnemyState>();

        attackColliderRight.enabled = false;
        attackColliderLeft.enabled = false;

        filter = new ContactFilter2D();
        filter.SetLayerMask(playerLayer);
        filter.useTriggers = true;
    }

    private void Update()
    {
        if (state.CurrentState != EnemyState.EnemyStateType.Attack)
            return;

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            StartCoroutine(DoAttack());
        }
    }
    private IEnumerator DoAttack()
    {
        Collider2D active = GetActiveCollider();

        // wait before hit
        yield return new WaitForSeconds(attackWindup);

        active.enabled = true;

        float timer = 0f;

        while (timer < attackDuration)
        {
            int count = active.Overlap(filter, results);

            for (int i = 0; i < count; i++)
            {
                PlayerHealth player = results[i].attachedRigidbody?.GetComponent<PlayerHealth>();

                if (player != null)
                {
                    player.TakeDamage(damage, transform.position);
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }

        active.enabled = false;
    }

    private Collider2D GetActiveCollider()
    {
        return state.IsFacingRight ? attackColliderRight : attackColliderLeft;
    }
}