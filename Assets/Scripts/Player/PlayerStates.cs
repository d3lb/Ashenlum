using UnityEngine;

public class PlayerState : MonoBehaviour
{
    public bool IsGrounded { get; set; }

    public bool IsJumping { get; set; }
    public bool IsWallJumping { get; set; }
    public bool IsFalling { get; set; }

    public bool IsDashing { get; set; }

    public bool IsSliding { get; set; }
    
    public bool IsAttacking { get; set; }
    public bool IsTakingDamage { get; set; }
    public bool IsDead { get; set; }

    public bool IsFacingRight { get; set; } = true;

    public bool IsBusy => IsDashing || IsAttacking || IsDead;
}
