using UnityEngine;
using System.Collections;

public class DashBruteAttackController : MonoBehaviour, IRespawnReset {
    [Header("References")]
    [SerializeField] private EnemyAnimation enemyAnimation;
    [SerializeField] private AfterImage afterImage;
    [SerializeField] private Collider2D leftDashHitbox;
    [SerializeField] private Collider2D rightDashHitbox;

    
    // Stands in for a prepare animation: the eyes light up, the body does not move.
    [Header("Telegraph")]
    [SerializeField] private GameObject telegraphPrefab;
    [SerializeField] private Transform leftTelegraphPoint;
    [SerializeField] private Transform rightTelegraphPoint;

    [Header("Edges")]
    [SerializeField] private CombatZone combatZone;

    [Header("General")]
    [SerializeField] private float globalAttackCooldown = 1f;
    [SerializeField] private float recoverTime = 0.45f;

    [Header("Dash Attack")]
    [SerializeField] private float dashWindup = 0.45f;
    [SerializeField] private float dashSpeed = 13f;

    // Multiplies dashSpeed across the dash. Front loaded, or it reads as sliding.
    [SerializeField] private AnimationCurve dashSpeedCurve =
        new AnimationCurve(new Keyframe(0f, 1.6f), new Keyframe(0.35f, 1f), new Keyframe(1f, 0.15f));
    [SerializeField] private float dashDuration = 0.45f;
    [SerializeField] private float dashEndLag = 0.25f;


    private EnemyState state;
    private Rigidbody2D rb;

    private float lastAttackTime;
    private bool isPerformingAttack;
    private GameObject telegraph;

    private void Awake() {
        state = GetComponent<EnemyState>();
        rb = GetComponent<Rigidbody2D>();

        ResetForRespawn();
    }

    // Coroutines die with the object; isPerformingAttack stuck true blocks CanAttack forever.
    public void ResetForRespawn() {
        isPerformingAttack = false;
        lastAttackTime = 0f;

        ClearTelegraph();
        enemyAnimation.SetDashing(false);

        if (afterImage != null) afterImage.Stop();

        leftDashHitbox.enabled = false;
        rightDashHitbox.enabled = false;
    }

    // Parented to the point, so it rides the head through the backstep.
    private void SpawnTelegraph() {
        ClearTelegraph();

        Transform point = state.IsFacingRight ? rightTelegraphPoint : leftTelegraphPoint;
        if (telegraphPrefab == null || point == null) return;

        telegraph = Instantiate(telegraphPrefab, point.position,
                                point.rotation * Quaternion.Euler(0f, 0f, 90f), point);
    }

    // Killed on the frame he commits, so the glow never overlaps the swing.
    private void ClearTelegraph() {
        if (telegraph != null) Destroy(telegraph);
        telegraph = null;
    }

    public bool CanAttack() {
        if (isPerformingAttack)
            return false;

        if (state.IsAttacking)
            return false;

        if (state.IsKnocked)
            return false;

        if (state.IsDead)
            return false;

        if (Time.time < lastAttackTime + globalAttackCooldown)
            return false;

        return true;
    }

    public void StartDashAttack(Transform target) {
        if (!CanAttack())
            return;

        StartCoroutine(DashAttackRoutine(target));
    }

    private IEnumerator DashAttackRoutine(Transform target) {
        BeginAttack();

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (target != null)
            state.IsFacingRight = target.position.x > transform.position.x;

        SpawnTelegraph();

        float direction = state.IsFacingRight ? 1f : -1f;

        yield return new WaitForSeconds(dashWindup);

        ClearTelegraph();
        enemyAnimation.SetDashing(true);

        if (afterImage != null) afterImage.Play();

        Collider2D activeHitbox = state.IsFacingRight ? rightDashHitbox : leftDashHitbox;

        activeHitbox.enabled = true;

        float boundaryX = direction > 0f ? combatZone.pointB.position.x : combatZone.pointA.position.x;

        float timer = 0f;

        while (timer < dashDuration) {
            if (direction > 0f && transform.position.x >= boundaryX)
                break;

            if (direction < 0f && transform.position.x <= boundaryX)
                break;

            timer += Time.deltaTime;

            float speed = dashSpeed * dashSpeedCurve.Evaluate(timer / dashDuration);
            rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);

            yield return null;
        }

        activeHitbox.enabled = false;
        enemyAnimation.SetDashing(false);

        if (afterImage != null) afterImage.Stop();

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        yield return new WaitForSeconds(dashEndLag);

        yield return RecoverRoutine();
    }

    private void BeginAttack() {
        isPerformingAttack = true;
        state.IsAttacking = true;
        state.CurrentState = EnemyState.EnemyStateType.Attack;

        lastAttackTime = Time.time;
    }

    private IEnumerator RecoverRoutine() {
        state.CurrentState = EnemyState.EnemyStateType.Recover;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        yield return new WaitForSeconds(recoverTime);

        state.IsAttacking = false;
        isPerformingAttack = false;

        state.CurrentState = EnemyState.EnemyStateType.Idle;
    }
}