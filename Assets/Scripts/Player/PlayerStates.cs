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
        Burst,
        Dead
    }

    public PlayerStateType CurrentState;

    public bool IsDashing;
    public bool IsSliding;
    public bool IsAttacking;
    public bool IsUsingAbility;
    public bool IsGrounded;

    public bool IsFacingRight { get; set; } = true;


    public bool IsBusy => IsDashing
                          || IsAttacking
                          || IsSliding
                          || IsUsingAbility
                          || CurrentState == PlayerStateType.Dead;
}
