using UnityEngine;

public class EnemyAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private EnemyState state;

    private void Update()
    {
        animator.SetBool(
            "IsPatroling",
            state.CurrentState == EnemyState.EnemyStateType.Patrol
        );

        animator.SetBool(
            "IsChasing",
            state.CurrentState == EnemyState.EnemyStateType.Chase
        );

        animator.SetBool(
            "IsRecovering",
            state.CurrentState == EnemyState.EnemyStateType.Recover
        );
    }

    public void TriggerHit()
    {
        animator.SetTrigger("Hit");
    }

    public void TriggerPrepare()
    {
        animator.SetTrigger("IsPreparing");
    }

    public void TriggerAttack()
    {
        animator.SetTrigger("IsAttacking");
    }

    public void TriggerSlam()
    {
        animator.SetTrigger("Slam");
    }

    public void TriggerCharge()
    {
        animator.SetTrigger("Charge");
    }
}