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
        Revcover
    }
    public EnemyStateType CurrentState;

    public bool IsFacingRight;
    public bool IsKnocked;
    public bool IsAttacking;
}