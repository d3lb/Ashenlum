using UnityEngine;

public class SecretaryBirdState : MonoBehaviour {
    public enum BossStateType {
        Intro,
        Idle,
        Choosing,
        Reposition,
        Windup,     
        Attacking,  
        Recover,    
        Hit,
        Dead
    }

    [Header("Runtime (read only)")]
    public BossStateType CurrentState = BossStateType.Idle;
    public bool IsDead;
    public bool IsFacingRight = true;
    public int Phase = 1;

    [Header("Flip")]
    [SerializeField] private Transform flipRoot;
    [SerializeField] private bool artFacesRight = true;

    public bool IsActing =>
        CurrentState != BossStateType.Idle && CurrentState != BossStateType.Dead;

    public bool IsVulnerableWindow => CurrentState == BossStateType.Recover;

    private void Awake() {
        if (flipRoot == null)
            Debug.LogError($"[SecretaryBirdState] '{name}' has no Flip Root, so he will never " +
                           "turn around.", this);
    }

    public void SetFacing(bool right) {
        IsFacingRight = right;
        if (flipRoot == null) return;

        Vector3 s = flipRoot.localScale;
        s.x = Mathf.Abs(s.x) * ((right == artFacesRight) ? 1f : -1f);
        flipRoot.localScale = s;
    }

    public void FaceTowards(float x) => SetFacing(x > transform.position.x);
}