using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMove : MonoBehaviour
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



    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        state = GetComponent<EnemyState>();
        health = GetComponent<EnemyHealth>();
    }

    void Update()
    {

        if (state.CurrentState == EnemyState.EnemyStateType.Hit && state.IsKnocked)
        {
            return;
        }

        Collider2D[] playerInSight = Physics2D.OverlapCircleAll(playerCheck.position, playerCheckRange, playerLayer);

        if (playerInSight.Length > 0)
        {
            float distance = Vector2.Distance(transform.position, playerInSight[0].transform.position);

            if (distance > playerStopCheckRange)
                state.CurrentState = EnemyState.EnemyStateType.Chase;
            else
                state.CurrentState = EnemyState.EnemyStateType.Attack;
        }
        else
        {
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
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        // flip 
        if (state.CurrentState != EnemyState.EnemyStateType.Hit)
        {
            if (rb.linearVelocity.x > 0.01f)
            {
                sprite.flipX = false;
                state.IsFacingRight = false;
            }
                
            else if (rb.linearVelocity.x < -0.01f)
            {
                sprite.flipX = true;
                state.IsFacingRight = true;
            }


        }
    }

    private void ChasePlayer()
    {
        Collider2D[] playerInSight = Physics2D.OverlapCircleAll(playerCheck.position, playerCheckRange, playerLayer);

        if (playerInSight.Length == 0)
            return;

        Transform player = playerInSight[0].transform;

        float dir = Mathf.Sign(player.position.x - transform.position.x);

        rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(playerCheck.position, playerCheckRange);
    }
}
