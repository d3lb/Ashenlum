using UnityEngine;

public class EnemyState : MonoBehaviour
{
    public enum EnemyStateType
    {
        Patrol,
        Chase,
        Attack,
        Hit,


        // Flying
        Recover,


        //Death
        Dead
    }
    public EnemyStateType CurrentState;

    public bool IsFacingRight;
    public bool IsKnocked;
    public bool IsAttacking;
    public bool IsDead;
}