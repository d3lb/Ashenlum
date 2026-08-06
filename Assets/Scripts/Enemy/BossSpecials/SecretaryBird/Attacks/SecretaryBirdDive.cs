using System.Collections;
using UnityEngine;

/// <summary>
/// Claim a high perch, flash a locked diagonal, blink into the floor.
///
/// The answer is "move sideways", not "jump" - a third distinct verb. The line is captured
/// once at the moment of commit, so stepping off it always works. That is what makes a
/// dive at this speed dodgeable rather than a coin flip.
/// </summary>
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
    [Tooltip("Slight gravity so the dive arcs and reads as weight, not as a laser.")]
    [SerializeField] private float diveGravity = 1.2f;

    public override string DisplayName => "Dive";

    public override IEnumerator Act(Transform player)
    {
        yield return MoveToWall(player, perchHeight);
        state.FaceTowards(player.position.x);
        yield return move.Hold(aimPause);

        // Lock on to where they are RIGHT NOW, then commit. No further tracking.
        Vector2 locked = new Vector2(Arena.ClampX(player.position.x), Arena.FloorY);
        yield return ShowPath(locked, telegraphTime);

        state.CurrentState = SecretaryBirdState.BossStateType.Attacking;
        hitboxes.EnableDiveHitbox();
        yield return move.Dash(locked, diveSpeed, diveGravity);
        hitboxes.DisableDiveHitbox();
    }
}
