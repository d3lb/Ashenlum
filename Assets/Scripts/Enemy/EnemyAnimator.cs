using UnityEngine;

public class EnemyAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private EnemyState state;
    [SerializeField] private Rigidbody2D rb;

    private void Update()
    {
        animator.SetBool(
            "IsWalking",
            state.CurrentState ==
            EnemyState.EnemyStateType.Patrol
        );

        animator.SetBool(
            "IsChasing",
            state.CurrentState ==
            EnemyState.EnemyStateType.Chase
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
}