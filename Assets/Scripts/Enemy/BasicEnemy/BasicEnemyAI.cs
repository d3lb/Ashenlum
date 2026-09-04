using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicEnemyAI : MonoBehaviour {
    [Header("Refrences")]
    [SerializeField] private CombatZone combatZone;
    [SerializeField] private Transform playerCheck;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask sightBlockLayer;

    [Header("Settings")]
    [SerializeField] private float speed = 2.5f;
    [SerializeField] private float playerCheckRange = 7f;
    [SerializeField] private float playerStopCheckRange = 3f;
    [SerializeField] private float loseTargetTime = 2f;


    [Header("Patrol")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float patrolSpeed = 1.5f;
    [SerializeField] private float arriveDistance = 0.1f;

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private EnemyState state;

    private Transform currentTarget;
    private Transform patrolTarget;
    private float loseTargetTimer;


    void Start() {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        state = GetComponent<EnemyState>();
        patrolTarget = pointB;
    }

    void Update() {
        if (state.IsDead)
            return;

        if (state.CurrentState == EnemyState.EnemyStateType.Hit
            && state.IsKnocked) {
            return;
        }

        if (currentTarget != null && !IsInsideCombatArea(currentTarget.position.x)) {
            currentTarget = null;
            loseTargetTimer = 0f;
            state.CurrentState = EnemyState.EnemyStateType.Patrol;
            return;
        }

        Collider2D[] playerInSight = Physics2D.OverlapCircleAll( playerCheck.position, playerCheckRange, playerLayer);

        if (playerInSight.Length > 0) {
            Transform target = playerInSight[0].transform;

            if (HasLineOfSight(target) &&
                IsInsideCombatArea(target.position.x)) {
                currentTarget = target;

                loseTargetTimer = loseTargetTime;

                float distance = Vector2.Distance(transform.position, currentTarget.position);

                if (distance > playerStopCheckRange)
                    state.CurrentState = EnemyState.EnemyStateType.Chase;
                else
                    state.CurrentState = EnemyState.EnemyStateType.Attack;

                return;
            }
        }

        // No valid target found
        if (loseTargetTimer > 0f) {
            loseTargetTimer -= Time.deltaTime;

            if (currentTarget != null) {
                float distance = Vector2.Distance(transform.position, currentTarget.position);

                if (distance > playerStopCheckRange)
                    state.CurrentState = EnemyState.EnemyStateType.Chase;
                else
                    state.CurrentState = EnemyState.EnemyStateType.Attack;
            }
        }
        else {
            currentTarget = null;
            state.CurrentState = EnemyState.EnemyStateType.Patrol;
        }
    }

    private void FixedUpdate() {
        if (state.IsDead) return;

        if (state.CurrentState == EnemyState.EnemyStateType.Hit)
            return;


        if (state.CurrentState == EnemyState.EnemyStateType.Patrol) {
            Patrol();
        }

        if (state.CurrentState == EnemyState.EnemyStateType.Chase) {
            ChasePlayer();
        }

        if (state.CurrentState == EnemyState.EnemyStateType.Attack) {
            if (!state.IsAttacking) {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }

        // flip 
        if (!state.IsAttacking) {
            bool facingRight = state.IsFacingRight;

            // face player if target exists
            if (currentTarget != null) {
                float deltaX =
                    currentTarget.position.x - transform.position.x;

                if (Mathf.Abs(deltaX) > 0.2f) {
                    facingRight = deltaX > 0f;
                }
            }
            // otherwise face movement direction
            else {
                float velocityX = rb.linearVelocity.x;

                if (Mathf.Abs(velocityX) > 0.05f) {
                    facingRight = velocityX > 0f;
                }
            }

            state.IsFacingRight = facingRight;
            sprite.flipX = !facingRight;
        }
    }

    private void ChasePlayer() {
        if (currentTarget == null)
            return;

        float deltaX =
            currentTarget.position.x - transform.position.x;

        if (Mathf.Abs(deltaX) < 0.2f) {
            rb.linearVelocity =
                new Vector2(0f, rb.linearVelocity.y);

            return;
        }

        float dir = Mathf.Sign(deltaX);

        rb.linearVelocity =
            new Vector2(dir * speed, rb.linearVelocity.y);
    }

    private void Patrol() {
        if (patrolTarget == null)
            return;

        float deltaX =
            patrolTarget.position.x - transform.position.x;

        if (Mathf.Abs(deltaX) < arriveDistance) {
            patrolTarget =
                patrolTarget == pointA
                ? pointB
                : pointA;

            return;
        }

        float dir = Mathf.Sign(deltaX);

        rb.linearVelocity =
            new Vector2(dir * patrolSpeed, rb.linearVelocity.y);
    }

    private bool HasLineOfSight(Transform target) {
        Vector2 origin = transform.position;
        Vector2 dir = (target.position - transform.position).normalized;

        float distance = Vector2.Distance(transform.position, target.position);

        RaycastHit2D hit = Physics2D.Raycast(origin, dir, distance, sightBlockLayer);

        return hit.collider == null;
    }

    private bool IsInsideCombatArea(float x) {
        if (combatZone == null)
            return true;

        float minX = Mathf.Min(combatZone.pointA.position.x, combatZone.pointB.position.x);
        float maxX = Mathf.Max(combatZone.pointA.position.x, combatZone.pointB.position.x);

        return x >= minX && x <= maxX;
    }

    private void OnDrawGizmosSelected() {
        if (playerCheck != null) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerCheck.position, playerCheckRange);
        }

        if (combatZone != null) {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(combatZone.pointA.position, combatZone.pointB.position);
        }
    }
}
