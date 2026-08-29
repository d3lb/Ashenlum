using UnityEngine;

// No Hit state, deliberately. The King never flinches - every other enemy in the game
// does, and his not doing so is the characterisation.
public class KingState : MonoBehaviour
{
    public enum KingStateType
    {
        Throne,
        Intro,
        Idle,
        Choosing,
        Windup,
        Attacking,
        Recover,
        Transition,
        Dead
    }

    [Header("Runtime (read only)")]
    public KingStateType CurrentState = KingStateType.Throne;
    public bool IsDead;
    public int Phase = 1;

    // He floats and never repositions, so facing is only for the art.
    public bool IsFacingRight = true;

    [Header("Flip")]
    [SerializeField] private Transform flipRoot;
    [SerializeField] private bool artFacesRight = true;

    public bool IsActing =>
        CurrentState != KingStateType.Idle &&
        CurrentState != KingStateType.Throne &&
        CurrentState != KingStateType.Dead;

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
