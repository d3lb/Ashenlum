using System.Collections;
using UnityEngine;

public class SecretaryBirdMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SecretaryBirdState state;
    [SerializeField] private SecretaryBirdAttackController attackController;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashPrepareTime = 0.35f;
    [SerializeField] private float dashRecoveryTime = 0.25f;

    [Header("Fly")]
    [SerializeField] private float flyHeight = 5f;
    [SerializeField] private float flySpeed = 10f;
    [SerializeField] private float flyPause = 0.2f;

    [Header("Dive")]
    [SerializeField] private float diveSpeed = 22f;
    [SerializeField] private float diveRecoveryTime = 0.3f;

    private bool movementFinished;

    public IEnumerator DashAttack(Transform target)
    {
        state.CurrentState = SecretaryBirdState.BossStateType.PrepareDash;

        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(dashPrepareTime);

        attackController.EnableDashHitbox();

        state.CurrentState = SecretaryBirdState.BossStateType.Dash;

        movementFinished = false;

        Vector2 direction =
            (target.position - transform.position).normalized;

        rb.linearVelocity = direction * dashSpeed;

        yield return new WaitUntil(() => movementFinished);

        attackController.DisableDashHitbox();

        rb.linearVelocity = Vector2.zero;

        state.CurrentState = SecretaryBirdState.BossStateType.Recover;

        yield return new WaitForSeconds(dashRecoveryTime);

        state.CurrentState = SecretaryBirdState.BossStateType.ChoosingAttack;
    }

    public IEnumerator FlyDiveAttack(Transform target)
    {
        state.CurrentState = SecretaryBirdState.BossStateType.Fly;

        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        Vector2 flyTarget =
            rb.position + Vector2.up * flyHeight;

        while (Vector2.Distance(rb.position, flyTarget) > 0.05f)
        {
            rb.MovePosition(
                Vector2.MoveTowards(
                    rb.position,
                    flyTarget,
                    flySpeed * Time.deltaTime
                )
            );

            yield return null;
        }

        Vector2 lockedTarget = target.position;

        yield return new WaitForSeconds(flyPause);

        state.CurrentState = SecretaryBirdState.BossStateType.Dive;

        movementFinished = false;

        Vector2 direction =
            (lockedTarget - rb.position).normalized;

        attackController.EnableDiveHitbox();

        rb.linearVelocity = direction * diveSpeed;

        yield return new WaitUntil(() => movementFinished);

        attackController.DisableDiveHitbox();

        rb.gravityScale = 1f;
        rb.linearVelocity = Vector2.zero;

        state.CurrentState = SecretaryBirdState.BossStateType.Recover;

        yield return new WaitForSeconds(diveRecoveryTime);

        state.CurrentState = SecretaryBirdState.BossStateType.ChoosingAttack;
    }

    public void FinishMovement()
    {
        movementFinished = true;
    }
}