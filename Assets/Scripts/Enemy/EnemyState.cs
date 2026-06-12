using UnityEngine;

public class EnemyState : MonoBehaviour
{
    public enum EnemyStateType
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Hit,
        Recover,
        Return,

        //Death
        Dead
    }
    public EnemyStateType CurrentState;

    public bool IsFacingRight;
    public bool IsKnocked;
    public bool IsAttacking;
    public bool IsDead;
}