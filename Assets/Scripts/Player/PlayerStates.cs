using UnityEngine;

public class PlayerState : MonoBehaviour
{

    public enum PlayerStateType
    {
        Idle,
        Run,
        Jump,
        Fall,
        Dash,
        WallSlide,
        Attack,
        Dead
    }

    public PlayerStateType CurrentState;

    public bool IsDashing;
    public bool IsSliding;

    public bool IsFacingRight { get; set; } = true;
    public bool IsBusy => CurrentState == PlayerStateType.Dash
                  || CurrentState == PlayerStateType.Attack
                  || CurrentState == PlayerStateType.Dead;
}
