using UnityEngine;

public class EnemyState : MonoBehaviour
{
    public enum EnemyStateType
    {
        Idle,
        Chase,
        Attack,
        Hit
    }

    public EnemyStateType CurrentState;

}
