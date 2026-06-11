using UnityEngine;

public class DashBruteAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerCheck;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask sightBlockLayer;
    [SerializeField] private DashBruteAttackController attackController;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 9f;
    [SerializeField] private float loseTargetRange = 12f;

    [Header("Attack Ranges")]
    [SerializeField] private float meleeAttackRange = 4f;
    [SerializeField] private float minDashRange = 5f;
    [SerializeField] private float dashAttackRange = 10f;

    [Header("Movement")]
    [SerializeField] private float chaseSpeed = 2.5f;

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private EnemyState state;

    private Transform currentTarget;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        state = GetComponent<EnemyState>();

        state.CurrentState = EnemyState.EnemyStateType.Idle;
    }

    private void Update()
    {
        if (state.IsDead)
            return;

        if (state.CurrentState == EnemyState.EnemyStateType.Hit && state.IsKnocked)
            return;

        if (state.CurrentState == EnemyState.EnemyStateType.Attack)
            return;

        if (state.CurrentState == EnemyState.EnemyStateType.Recover)
            return;

        if (currentTarget == null)
        {
            TryFindPlayer();
        }
        else
        {
            HandleCombatDecision();
        }

        UpdateFacing();
    }

    private void FixedUpdate()
    {
        if (state.IsDead)
            return;

        if (state.CurrentState == EnemyState.EnemyStateType.Chase)
        {
            ChasePlayer();
        }
        else if (
            state.CurrentState != EnemyState.EnemyStateType.Attack &&
            state.CurrentState != EnemyState.EnemyStateType.Hit
        )
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    private void TryFindPlayer()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            playerCheck.position,
            detectionRange,
            playerLayer
        );

        if (hits.Length == 0)
        {
            state.CurrentState = EnemyState.EnemyStateType.Idle;
            return;
        }

        Transform possibleTarget = hits[0].transform;

        if (HasLineOfSight(possibleTarget))
        {
            currentTarget = possibleTarget;
            state.CurrentState = EnemyState.EnemyStateType.Chase;
        }
    }

    private void HandleCombatDecision()
    {
        float distance = Vector2.Distance(transform.position, currentTarget.position);



        if (distance > loseTargetRange)
        {
            currentTarget = null;
            state.CurrentState = EnemyState.EnemyStateType.Idle;
            return;
        }

        if (!HasLineOfSight(currentTarget))
        {
            currentTarget = null;
            state.CurrentState = EnemyState.EnemyStateType.Idle;
            return;
        }

        if (!attackController.CanAttack())
        {
            state.CurrentState = EnemyState.EnemyStateType.Chase;
            return;
        }

        if (distance <= meleeAttackRange)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            attackController.StartMeleeAttack();
            return;
        }

        if (distance >= minDashRange && distance <= dashAttackRange)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            attackController.StartDashAttack(currentTarget);
            return;
        }

        state.CurrentState = EnemyState.EnemyStateType.Chase;
    }

    private void ChasePlayer()
    {
        if (currentTarget == null)
            return;

        float deltaX = currentTarget.position.x - transform.position.x;

        if (Mathf.Abs(deltaX) <= meleeAttackRange)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float dir = Mathf.Sign(deltaX);

        rb.linearVelocity = new Vector2(
            dir * chaseSpeed,
            rb.linearVelocity.y
        );
    }

    private bool HasLineOfSight(Transform target)
    {
        Vector2 origin = transform.position;
        Vector2 direction = (target.position - transform.position).normalized;

        float distance = Vector2.Distance(
            transform.position,
            target.position
        );

        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            direction,
            distance,
            sightBlockLayer
        );

        return hit.collider == null;
    }

    private void UpdateFacing()
    {
        if (currentTarget == null)
            return;

        bool facingRight = currentTarget.position.x > transform.position.x;

        state.IsFacingRight = facingRight;
        sprite.flipX = !facingRight;
    }

    private void OnDrawGizmosSelected()
    {
        if (playerCheck == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerCheck.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(playerCheck.position, meleeAttackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(playerCheck.position, dashAttackRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(playerCheck.position, loseTargetRange);
    }
}