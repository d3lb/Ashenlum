using UnityEngine;

/// <summary>
/// What the BIRD is doing. Nothing in here describes what the screen is showing.
///
/// The trajectory line is a pure view concern: attacks call SecretaryBirdTelegraph
/// directly, and no state, animation or gameplay check ever reads it. If you deleted
/// the LineRenderer entirely the fight would still run identically - it would just be
/// unreadable to a human. That is the correct relationship between the two.
/// </summary>
public class SecretaryBirdState : MonoBehaviour
{
    public enum BossStateType
    {
        Idle,        // between attacks
        Choosing,    // picking the next move
        Reposition,  // flying to a perch, no hitbox
        Windup,      // crouched / wings spread, committed but not yet dangerous
        Attacking,   // hitbox live
        Recover,     // THE punish window
        Hit,
        Dead
    }

    [Header("Runtime (read only)")]
    public BossStateType CurrentState = BossStateType.Idle;
    public bool IsDead;
    public bool IsFacingRight = true;
    public int Phase = 1;

    [Header("Flip")]
    [Tooltip("Usually the sprite child. Leave empty if the animator handles flipping.")]
    [SerializeField] private Transform flipRoot;
    [SerializeField] private bool artFacesRight = true;

    /// <summary>For animation only. Never gate attack selection on this.</summary>
    public bool IsActing =>
        CurrentState != BossStateType.Idle && CurrentState != BossStateType.Dead;

    /// <summary>The only window the player is meant to land free hits in.</summary>
    public bool IsVulnerableWindow => CurrentState == BossStateType.Recover;

    public void SetFacing(bool right)
    {
        IsFacingRight = right;
        if (flipRoot == null) return;

        Vector3 s = flipRoot.localScale;
        s.x = Mathf.Abs(s.x) * ((right == artFacesRight) ? 1f : -1f);
        flipRoot.localScale = s;
    }

    public void FaceTowards(float x) => SetFacing(x > transform.position.x);
}