using UnityEngine;
using System.Collections;

public class DashBruteAttackController : MonoBehaviour, IRespawnReset
{
    [Header("References")]
    [SerializeField] private EnemyAnimation enemyAnimation;
    [SerializeField] private Collider2D leftMeleeHitbox;
    [SerializeField] private Collider2D rightMeleeHitbox;
    [SerializeField] private Collider2D leftDashHitbox;
    [SerializeField] private Collider2D rightDashHitbox;

    
    [Header("Edges")]
    [SerializeField] private CombatZone combatZone;

    [Header("General")]
    [SerializeField] private float globalAttackCooldown = 1f;
    [SerializeField] private float recoverTime = 0.45f;

    [Header("Melee Attack")]
    [SerializeField] private float meleeWindup = 0.25f;
    [SerializeField] private float meleeActiveTime = 0.15f;
    [SerializeField] private float meleeLungeForce = 4f;

    [Header("Dash Attack")]
    [SerializeField] private float dashWindup = 0.45f;
    [SerializeField] private float dashBackstepForce = 3f;
    [SerializeField] private float dashSpeed = 13f;
    [SerializeField] private float dashDuration = 0.45f;
    [SerializeField] private float dashEndLag = 0.25f;


    private EnemyState state;
    private Rigidbody2D rb;

    private float lastAttackTime;
    private bool isPerformingAttack;

    private void Awake()
    {
        state = GetComponent<EnemyState>();
        rb = GetComponent<Rigidbody2D>();

        ResetForRespawn();
    }

    // Dying mid-attack kills the routine before it can clear these, and isPerformingAttack
    // stuck true means CanAttack never passes again.
    public void ResetForRespawn()
    {
        isPerformingAttack = false;
        lastAttackTime = 0f;

        leftMeleeHitbox.enabled = false;
        rightMeleeHitbox.enabled = false;
        leftDashHitbox.enabled = false;
        rightDashHitbox.enabled = false;
    }

    public bool CanAttack()
    {
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

    public void StartMeleeAttack()
    {
        if (!CanAttack())
            return;

        StartCoroutine(MeleeAttackRoutine());
    }

    public void StartDashAttack(Transform target)
    {
        if (!CanAttack())
            return;

        StartCoroutine(DashAttackRoutine(target));
    }

    private IEnumerator MeleeAttackRoutine()
    {
        BeginAttack();

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        enemyAnimation.TriggerPrepare();

        yield return new WaitForSeconds(meleeWindup);

        enemyAnimation.TriggerAttack();

        Collider2D activeHitbox = state.IsFacingRight ? rightMeleeHitbox : leftMeleeHitbox;

        float direction = state.IsFacingRight ? 1f : -1f;

        rb.AddForce(
            new Vector2(direction * meleeLungeForce, 0f),
            ForceMode2D.Impulse
        );

        activeHitbox.enabled = true;

        yield return new WaitForSeconds(meleeActiveTime);

        activeHitbox.enabled = false;

        yield return RecoverRoutine();
    }

    private IEnumerator DashAttackRoutine(Transform target)
    {
        BeginAttack();

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (target != null)
            state.IsFacingRight = target.position.x > transform.position.x;

        enemyAnimation.TriggerPrepare();

        float direction = state.IsFacingRight ? 1f : -1f;

        float minX = Mathf.Min(combatZone.pointA.position.x, combatZone.pointB.position.x);
        float maxX = Mathf.Max(combatZone.pointA.position.x, combatZone.pointB.position.x);

        if (direction > 0f && transform.position.x > minX + 0.5f)
        {
            rb.AddForce(new Vector2(-direction * dashBackstepForce, 0f), ForceMode2D.Impulse);
        }

        else if (direction < 0f && transform.position.x < maxX - 0.5f)
        {
            rb.AddForce(new Vector2(-direction * dashBackstepForce, 0f), ForceMode2D.Impulse);
        }

        yield return new WaitForSeconds(dashWindup);

        enemyAnimation.TriggerAttack();

        Collider2D activeHitbox = state.IsFacingRight ? rightDashHitbox : leftDashHitbox;

        activeHitbox.enabled = true;

        float boundaryX = direction > 0f ? combatZone.pointB.position.x : combatZone.pointA.position.x;

        float timer = 0f;

        while (timer < dashDuration)
        {
            if (direction > 0f && transform.position.x >= boundaryX)
                break;

            if (direction < 0f && transform.position.x <= boundaryX)
                break;

            timer += Time.deltaTime;
            rb.linearVelocity = new Vector2(direction * dashSpeed, rb.linearVelocity.y);

            yield return null;
        }

        activeHitbox.enabled = false;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        yield return new WaitForSeconds(dashEndLag);

        yield return RecoverRoutine();
    }

    private void BeginAttack()
    {
        isPerformingAttack = true;
        state.IsAttacking = true;
        state.CurrentState = EnemyState.EnemyStateType.Attack;

        lastAttackTime = Time.time;
    }

    private IEnumerator RecoverRoutine()
    {
        state.CurrentState = EnemyState.EnemyStateType.Recover;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        yield return new WaitForSeconds(recoverTime);

        state.IsAttacking = false;
        isPerformingAttack = false;

        state.CurrentState = EnemyState.EnemyStateType.Idle;
    }
}