using System.Collections;
using UnityEngine;

public class SecretaryBirdDive : SecretaryBirdAttack
{
    [Header("Perch")]
    [SerializeField, Range(0f, 1f)] private float perchHeight = 0.85f;
    [SerializeField] private float aimPause = 0.1f;

    [Header("Telegraph")]
    [Tooltip("The locked line IS the dodge window. Never set it to zero.")]
    [SerializeField] private float telegraphTime = 0.4f;

    [Header("Dive")]
    [SerializeField] private float diveSpeed = 56f;
    [Tooltip("Keep at 0. Any arc curves the path BELOW the straight line the telegraph drew, " +
             "so he clips the floor early and lands short of the player - and the line lies.")]
    [SerializeField] private float diveGravity = 0f;
    public override string DisplayName => "Dive";

    public override IEnumerator Act(Transform player)
    {
        yield return MoveToWall(player, perchHeight);
        state.FaceTowards(player.position.x);
        yield return move.Hold(aimPause);

        // Always the floor beneath them, never their airborne position. A bird that dives to
        // head height and stops mid-air reads as a glitch, and it lets a jumping player bait
        // the dive upward and land behind it for free.
        Vector2 target = new Vector2(Arena.ClampX(player.position.x), Arena.FloorY);
        yield return ShowPath(target, telegraphTime);

        state.CurrentState = SecretaryBirdState.BossStateType.Attacking;

        // The SIDE hitbox, not the dive one. The line runs from the centre of a big box to
        // the player, so the box floor-clips before its centre ever arrives - a hitbox under
        // the body always lands short. The side box leads the travel direction and connects.
        // Facing must be set before enabling, because EnableDashHitbox reads it.
        state.SetFacing(target.x > move.Position.x);
        hitboxes.EnableDashHitbox();
        yield return move.Dash(target, Speed(diveSpeed), diveGravity);
        hitboxes.DisableDashHitbox();
    }
}
