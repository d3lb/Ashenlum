using UnityEngine;

public class SecretaryBirdState : MonoBehaviour
{
    public enum BossStateType
    {
        Idle,
        ChoosingAttack,

        PrepareDash,
        Dash,

        Fly,
        Dive,

        Combo,

        Recover,

        Hit,
        Dead
    }

    public BossStateType CurrentState = BossStateType.Idle;

    public bool IsDead;
    public bool IsAttacking;
    public bool IsRecovering;
    public bool IsKnocked;
    public bool IsFacingRight = true;

    public bool IsBusy =>
    CurrentState == BossStateType.PrepareDash ||
    CurrentState == BossStateType.Dash ||
    CurrentState == BossStateType.Fly ||
    CurrentState == BossStateType.Dive ||
    CurrentState == BossStateType.Combo ||
    CurrentState == BossStateType.Recover;
}