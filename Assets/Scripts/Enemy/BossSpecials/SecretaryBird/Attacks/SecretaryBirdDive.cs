using System.Collections;
using UnityEngine;

public class SecretaryBirdDive : SecretaryBirdAttack
{
    [Header("Perch")]
    [SerializeField, Range(0f, 1f)] private float perchHeight = 0.85f;
    [SerializeField] private float aimPause = 0.1f;

    [Header("Telegraph")]
    [SerializeField] private float telegraphTime = 0.4f;

    [Header("Dive")]
    [SerializeField] private float diveSpeed = 56f;
    [SerializeField] private float diveGravity = 0f;

    // Aiming exactly at the player makes him land on the spot they just left.
    [SerializeField] private float overshoot = 3f;

    public override string DisplayName => "Dive";

    public override IEnumerator Act(Transform player)
    {
        yield return MoveToWall(player, perchHeight);
        state.FaceTowards(player.position.x);
        yield return move.Hold(aimPause);

        // The floor under them, never their airborne position - a jump would bait the dive up.
        Vector2 aim = new Vector2(Arena.ClampX(player.position.x), Arena.FloorY);

        float dir = Mathf.Sign(player.position.x - move.Position.x);
        Vector2 target = new Vector2(
            Arena.ClampX(player.position.x + dir * overshoot),
            Arena.FloorY);

        yield return ShowPath(aim, telegraphTime);

        state.CurrentState = SecretaryBirdState.BossStateType.Attacking;

        // Side hitbox, not dive: a box under the body floor-clips before its centre arrives.
        // Facing must be set first - EnableDashHitbox reads it.
        state.SetFacing(target.x > move.Position.x);
        hitboxes.EnableDashHitbox();
        yield return move.Dash(target, Speed(diveSpeed), diveGravity);
        hitboxes.DisableDashHitbox();
    }
}
