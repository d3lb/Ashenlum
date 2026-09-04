using System.Collections;
using UnityEngine;

public class SecretaryBirdWallDash : SecretaryBirdAttack {
    [Header("Perch")]
    [SerializeField, Range(0f, 1f)] private float perchHeight = 0.05f;
    [SerializeField] private float perchPause = 0.08f;

    [Header("Telegraph")]
    [SerializeField] private float telegraphTime = 0.4f;
    [SerializeField, Range(0.1f, 1f)] private float chainTelegraphScale = 0.4f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 52f;
    [SerializeField, Range(1, 4)] private int dashes = 1;
    [SerializeField] private float betweenDashes = 0.1f;
    [SerializeField] private bool aimAtPlayer = false;

    public override string DisplayName => dashes > 1 ? $"Wall Dash x{dashes}" : "Wall Dash";

    public override IEnumerator Act(Transform player) {
        yield return MoveToWall(player, perchHeight);
        yield return move.Hold(perchPause);

        int side = CurrentSide;

        for (int i = 0; i < dashes; i++) {
            Vector2 from = move.Position;
            Vector2 to = TargetFrom(player, side, from);

            yield return ShowPath(to, telegraphTime * (i == 0 ? 1f : chainTelegraphScale));

            state.CurrentState = SecretaryBirdState.BossStateType.Attacking;
            state.SetFacing(to.x > from.x);
            hitboxes.EnableDashHitbox();
            yield return move.Dash(to, Speed(dashSpeed));
            hitboxes.DisableDashHitbox();

            side = -side;
            if (i < dashes - 1) yield return move.Hold(betweenDashes);
        }
    }

    private Vector2 TargetFrom(Transform player, int fromSide, Vector2 from) {
        float x = Arena.WallX(-fromSide);
        float y = aimAtPlayer ? Arena.ClampY(player.position.y) : from.y;
        return new Vector2(x, y);
    }
}
