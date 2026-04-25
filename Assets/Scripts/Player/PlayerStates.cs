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
        SideAttack,
        UpAttack,
        DownAttack,
        Dead
    }

    public PlayerStateType CurrentState;

    public bool IsDashing;
    public bool IsSliding;
    public bool IsAttacking;

    public bool IsGrounded;

    public bool IsFacingRight { get; set; } = true;


    public bool IsBusy => CurrentState == PlayerStateType.Dash
                          || IsAttacking 
                          || CurrentState == PlayerStateType.Dead;
}
