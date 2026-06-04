using UnityEngine;

public class FlyingEnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private EnemyAnimation enemyAnimation;

    [Header("Patrol")]
    [SerializeField] private Transform patrolPointA;
    [SerializeField] private Transform patrolPointB;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float patrolHoverAmplitude = 0.3f;   // how much it wiggles up/down
    [SerializeField] private float patrolHoverFrequency = 2f;     // how fast it wiggles

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float hoverAbovePlayerHeight = 2.5f; // how high above player it hovers

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 6f;
    [SerializeField] private LayerMask obstacleMask;               // walls/terrain for raycast
    [SerializeField] private float losePlayerRadius = 9f;          // when to give up chasing

    [Header("Recover")]
    [SerializeField] private float recoverSpeed = 4f;
    [SerializeField] private float recoverDistance = 3f;           // how far back it pulls away
    [SerializeField] private float recoverHeight = 2f;             // height it climbs back to
    [SerializeField] private float recoverDuration = 1.2f;         // max time in recover state

    private EnemyState state;
    private Rigidbody2D rb;
    private SpriteRenderer sprite;

    private Transform currentPatrolTarget;
    private float hoverTimer;
    private float recoverTimer;
    private Vector2 recoverTarget;

    private void Awake()
    {
        state = GetComponent<EnemyState>();
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();

        // Start patrolling toward A
        currentPatrolTarget = patrolPointA;
        state.CurrentState = EnemyState.EnemyStateType.Patrol;
    }

    private void Update()
    {
        // Hit state is controlled by EnemyHealth, don't interrupt it
        if (state.CurrentState == EnemyState.EnemyStateType.Hit)
            return;

        // Attack state is controlled by FlyingEnemyAttack
        if (state.CurrentState == EnemyState.EnemyStateType.Attack)
            return;

        switch (state.CurrentState)
        {
            case EnemyState.EnemyStateType.Patrol:
                HandlePatrol();
                TryDetectPlayer();
                break;

            case EnemyState.EnemyStateType.Chase:
                HandleChase();
                CheckLostPlayer();
                break;

            case EnemyState.EnemyStateType.Revcover:
                HandleRecover();
                break;
        }

        UpdateFacing();
    }

    //  PATROL 

    private void HandlePatrol()
    {
        hoverTimer += Time.deltaTime;

        Vector2 target = currentPatrolTarget.position;

        // Add sine wave wiggle on Y
        float wiggle = Mathf.Sin(hoverTimer * patrolHoverFrequency) * patrolHoverAmplitude;
        target.y += wiggle;

        MoveToward(target, patrolSpeed);

        // Flip patrol point when close enough on X
        if (Mathf.Abs(transform.position.x - currentPatrolTarget.position.x) < 0.3f)
        {
            currentPatrolTarget = currentPatrolTarget == patrolPointA
                ? patrolPointB
                : patrolPointA;
        }
    }

    //  DETECTION 

    private void TryDetectPlayer()
    {
        if (playerTransform == null) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist > detectionRadius) return;

        // Raycast to check line of sight
        Vector2 dirToPlayer = (playerTransform.position - transform.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            dirToPlayer,
            detectionRadius,
            obstacleMask
        );

        // If raycast hits nothing (no wall in the way), player is visible
        if (!hit.collider)
        {
            state.CurrentState = EnemyState.EnemyStateType.Chase;
        }
    }

    private void CheckLostPlayer()
    {
        if (playerTransform == null) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist > losePlayerRadius)
        {
            state.CurrentState = EnemyState.EnemyStateType.Patrol;
        }
    }

    //  CHASE

    private void HandleChase()
    {
        if (playerTransform == null) return;

        // Target position: above the player
        Vector2 target = new Vector2(
            playerTransform.position.x,
            playerTransform.position.y + hoverAbovePlayerHeight
        );

        MoveToward(target, chaseSpeed);
    }

    // Called by FlyingEnemyAttack when the dive is done
    public void StartRecover()
    {
        recoverTimer = 0f;

        // Pull back horizontally away from player, and rise up
        float dirAway = state.IsFacingRight ? -1f : 1f;
        recoverTarget = new Vector2(
            transform.position.x + dirAway * recoverDistance,
            (playerTransform != null ? playerTransform.position.y : transform.position.y)
                + recoverHeight
        );

        state.CurrentState = EnemyState.EnemyStateType.Revcover;
    }

    //  RECOVER

    private void HandleRecover()
    {
        recoverTimer += Time.deltaTime;

        MoveToward(recoverTarget, recoverSpeed);

        bool reachedTarget = Vector2.Distance(transform.position, recoverTarget) < 0.3f;
        bool timedOut = recoverTimer >= recoverDuration;

        if (reachedTarget || timedOut)
        {
            // Decide next state
            if (playerTransform != null)
            {
                float dist = Vector2.Distance(transform.position, playerTransform.position);
                state.CurrentState = dist <= losePlayerRadius
                    ? EnemyState.EnemyStateType.Chase
                    : EnemyState.EnemyStateType.Patrol;
            }
            else
            {
                state.CurrentState = EnemyState.EnemyStateType.Patrol;
            }
        }
    }

    // HELPERS

    private void MoveToward(Vector2 target, float speed)
    {
        Vector2 newPos = Vector2.MoveTowards(
            transform.position,
            target,
            speed * Time.deltaTime
        );
        rb.MovePosition(newPos);
    }

    private void UpdateFacing()
    {
        if (playerTransform == null) return;

        if (state.CurrentState == EnemyState.EnemyStateType.Patrol)
        {
            state.IsFacingRight = currentPatrolTarget.position.x > transform.position.x;
        }
        else
        {
            state.IsFacingRight = playerTransform.position.x > transform.position.x;
        }

        sprite.flipX = !state.IsFacingRight;
    }

    //  GIZMOS

    private void OnDrawGizmosSelected()
    {
        // Detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Lose player radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, losePlayerRadius);

        // Patrol points
        if (patrolPointA != null && patrolPointB != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(patrolPointA.position, patrolPointB.position);
            Gizmos.DrawSphere(patrolPointA.position, 0.2f);
            Gizmos.DrawSphere(patrolPointB.position, 0.2f);
        }
    }
}