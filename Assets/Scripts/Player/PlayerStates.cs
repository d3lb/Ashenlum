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

    public PlayerStateType CurrentState { get; set; }

    public bool IsDashing { get; set; }
    public bool IsSliding { get; set; }
    public bool IsAttacking { get; set; }
    public bool IsUsingAbility { get; set; }
    public bool IsGrounded { get; set; }

    public bool IsFacingRight { get; set; } = true;


    public bool IsBusy => IsDashing
                          || IsAttacking
                          || IsUsingAbility
                          || CurrentState == PlayerStateType.Dead;
}
