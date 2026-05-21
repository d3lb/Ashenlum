using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicEnemyAI : MonoBehaviour
{
    [SerializeField] private Transform playerCheck;
    [SerializeField] private float speed = 2.5f;
    [SerializeField] private float playerCheckRange = 7f;
    [SerializeField] private float playerStopCheckRange = 3f;
    [SerializeField] private LayerMask playerLayer;

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private EnemyState state;
    private EnemyHealth health;

    private Transform currentTarget;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        state = GetComponent<EnemyState>();
        health = GetComponent<EnemyHealth>();
    }

    void Update()
    {
        if (state.CurrentState == EnemyState.EnemyStateType.Hit
            && state.IsKnocked)
        {
            return;
        }

        Collider2D[] playerInSight =
            Physics2D.OverlapCircleAll(
                playerCheck.position,
                playerCheckRange,
                playerLayer
            );

        if (playerInSight.Length > 0)
        {
            currentTarget = playerInSight[0].transform;

            float distance =
                Vector2.Distance(
                    transform.position,
                    currentTarget.position
                );

            if (distance > playerStopCheckRange)
                state.CurrentState = EnemyState.EnemyStateType.Chase;
            else
                state.CurrentState = EnemyState.EnemyStateType.Attack;
        }
        else
        {
            currentTarget = null;
            state.CurrentState = EnemyState.EnemyStateType.Idle;
        }
    }

    private void FixedUpdate()
    {

        if (state.CurrentState == EnemyState.EnemyStateType.Hit)
            return;


        if (state.CurrentState == EnemyState.EnemyStateType.Idle)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        if (state.CurrentState == EnemyState.EnemyStateType.Chase)
        {
            ChasePlayer();
        }

        if (state.CurrentState == EnemyState.EnemyStateType.Attack)
        {
            if (!state.IsAttacking)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }

        // flip 
        if (currentTarget != null && !state.IsAttacking)
        {
            bool facingRight =
                currentTarget.position.x > transform.position.x;

            state.IsFacingRight = facingRight;
            sprite.flipX = !facingRight;
        }
    }

    private void ChasePlayer()
    {
        if (currentTarget == null)
            return;

        float dir =
            Mathf.Sign(
                currentTarget.position.x - transform.position.x
            );

        rb.linearVelocity =
            new Vector2(
                dir * speed,
                rb.linearVelocity.y
            );
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(playerCheck.position, playerCheckRange);
    }
}
