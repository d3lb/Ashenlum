using UnityEngine;

// No Hit state: he never flinches.
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

    // Art only; he never repositions.
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
