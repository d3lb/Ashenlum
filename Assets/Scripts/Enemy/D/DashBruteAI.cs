using System.Collections;
using UnityEngine;

// He has no walk. The dash is the only way he travels, toward the player or back to his post.
public class DashBruteAI : MonoBehaviour {
    [Header("References")]
    [SerializeField] private Transform playerCheck;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask sightBlockLayer;
    [SerializeField] private DashBruteAttackController attackController;
    [SerializeField] private EnemyAnimation enemyAnimation;

    [Header("Edges")]
    [SerializeField] private CombatZone combatZone;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 9f;
    [SerializeField] private float loseTargetRange = 12f;

    [Header("Attack Ranges")]
    [SerializeField] private float dashAttackRange = 10f;

    [Header("Movement")]
    [SerializeField] private float returnDashSpeed = 13f;

    // A wall between him and his post would otherwise hold him in the dash forever.
    [SerializeField] private float returnTimeout = 3f;

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private EnemyState state;

    private Transform currentTarget;
    private Vector2 homePosition;
    private bool returning;


    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        state = GetComponent<EnemyState>();

        homePosition = transform.position;

        state.CurrentState = EnemyState.EnemyStateType.Idle;
    }

    private void Update() {
        if (state.IsDead)
            return;

        if (returning)
            return;

        if (state.CurrentState == EnemyState.EnemyStateType.Hit && state.IsKnocked)
            return;

        // IsAttacking, not CurrentState: EnemyHealth resets CurrentState to Patrol when the
        // knockback ends, which would hand control back mid-attack.
        if (state.IsAttacking)
            return;

        if (state.CurrentState == EnemyState.EnemyStateType.Recover)
            return;

        if (currentTarget == null) {
            TryFindPlayer();
        }
        else {
            HandleCombatDecision();
        }

        UpdateFacing();
    }

    // Standing is the default: everything that is not an attack or a return holds him still.
    private void FixedUpdate() {
        if (state.IsDead)
            return;

        if (returning)
            return;

        // The attack owns his velocity for its whole length, knockback or not.
        if (state.IsAttacking)
            return;

        if (state.CurrentState != EnemyState.EnemyStateType.Hit) {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    private void TryFindPlayer() {

        Collider2D[] hits = Physics2D.OverlapCircleAll(playerCheck.position, detectionRange, playerLayer);

        if (hits.Length == 0) {
            state.CurrentState = EnemyState.EnemyStateType.Idle;
            return;
        }

        Transform possibleTarget = hits[0].transform;

        if (HasLineOfSight(possibleTarget) && IsInsideCombatArea(possibleTarget.position.x)) {
            currentTarget = possibleTarget;
            state.CurrentState = EnemyState.EnemyStateType.Idle;
        }
    }

    private void HandleCombatDecision() {


        if (!IsInsideCombatArea(currentTarget.position.x)) {
            currentTarget = null;
            BeginReturn();
            return;
        }

        float distance = Vector2.Distance(transform.position, currentTarget.position);

        if (distance > loseTargetRange) {
            currentTarget = null;
            BeginReturn();
            return;
        }

        if (!HasLineOfSight(currentTarget)) {
            currentTarget = null;
            BeginReturn();
            return;
        }

        // Cooling down. He cannot close the gap on foot, so he waits where he is.
        if (!attackController.CanAttack()) {
            state.CurrentState = EnemyState.EnemyStateType.Idle;
            return;
        }

        if (distance <= dashAttackRange) {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            attackController.StartDashAttack(currentTarget);
            return;
        }

        state.CurrentState = EnemyState.EnemyStateType.Idle;
    }

    private void BeginReturn() {
        if (returning)
            return;

        if (Mathf.Abs(homePosition.x - transform.position.x) < 0.2f) {
            state.CurrentState = EnemyState.EnemyStateType.Idle;
            return;
        }

        StartCoroutine(ReturnDash());
    }

    // One burst back to his post, turning his back on the player, reusing the dash clip.
    private IEnumerator ReturnDash() {
        returning = true;
        state.CurrentState = EnemyState.EnemyStateType.Return;

        float dir = Mathf.Sign(homePosition.x - transform.position.x);

        state.IsFacingRight = dir > 0f;
        sprite.flipX = !state.IsFacingRight;

        enemyAnimation.SetDashing(true);

        float elapsed = 0f;

        // On the sign, not the distance: overshooting by more than the threshold in one frame
        // would otherwise send him running the same way forever.
        while (Mathf.Sign(homePosition.x - transform.position.x) == dir && elapsed < returnTimeout) {
            if (state.IsDead || state.IsKnocked)
                break;

            elapsed += Time.deltaTime;
            rb.linearVelocity = new Vector2(dir * returnDashSpeed, rb.linearVelocity.y);

            yield return null;
        }

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        enemyAnimation.SetDashing(false);

        returning = false;
        state.CurrentState = EnemyState.EnemyStateType.Idle;
    }

    // The coroutine dies with the object, leaving the flag and the dash bool stuck on.
    private void OnDisable() {
        if (!returning)
            return;

        returning = false;
        enemyAnimation.SetDashing(false);
    }

    private bool HasLineOfSight(Transform target) {
        Vector2 origin = transform.position;
        Vector2 direction = (target.position - transform.position).normalized;

        float distance = Vector2.Distance(transform.position, target.position);

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, sightBlockLayer);

        return hit.collider == null;
    }

    private bool IsInsideCombatArea(float x) {
        float minX = Mathf.Min(combatZone.pointA.position.x, combatZone.pointB.position.x);
        float maxX = Mathf.Max(combatZone.pointA.position.x, combatZone.pointB.position.x);

        return x >= minX && x <= maxX;
    }

    private void UpdateFacing() {
        if (currentTarget == null)
            return;

        bool facingRight = currentTarget.position.x > transform.position.x;

        state.IsFacingRight = facingRight;
        sprite.flipX = !facingRight;
    }

    private void OnDrawGizmosSelected() {
        if (playerCheck == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(playerCheck.position, detectionRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(playerCheck.position, dashAttackRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(playerCheck.position, loseTargetRange);
    }
}