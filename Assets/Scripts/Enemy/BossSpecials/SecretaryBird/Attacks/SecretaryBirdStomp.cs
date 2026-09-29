using System.Collections;
using UnityEngine;

public class SecretaryBirdStomp : SecretaryBirdAttack {
    [Header("Rise")]
    [SerializeField, Range(0f, 1f)] private float riseHeight = 0.9f;
    [SerializeField] private float hangTime = 0.12f;

    [Header("Telegraph")]
    [SerializeField] private float telegraphTime = 0.25f;
    [SerializeField] private float markTime = 0.28f;

    [Header("Slam")]
    [SerializeField] private float slamSpeed = 62f;
    [SerializeField] private float slamGravity = 2.5f;

    [Header("Shockwave")]
    [SerializeField] private GameObject shockwavePrefab;
    [SerializeField] private float shockwaveYOffset = 0.4f;
    [SerializeField] private bool bothDirections = true;

    [Header("Buried")]
    [SerializeField] private float buriedTime = 0.9f;

    public override string DisplayName => "Stomp";

    public override IEnumerator Act(Transform player) {
        Vector2 above = new Vector2(
            Arena.ClampX(player.position.x),
            Mathf.Lerp(Arena.FloorY, Arena.CeilY, riseHeight));

        yield return BlinkTo(above);
        state.FaceTowards(player.position.x);
        yield return move.Hold(hangTime);

        Vector2 target = new Vector2(move.Position.x, Arena.FloorY);

        // The coil is the whole telegraph: he hangs in it for as long as the warning lasts.
        if (anim != null) anim.SetStomping(true);

        yield return ShowPath(target, telegraphTime);

        yield return move.Hold(markTime * Pace.telegraphScale);

        state.CurrentState = SecretaryBirdState.BossStateType.Attacking;

        if (anim != null) anim.SetStomping(false);

        hitboxes.EnableDiveHitbox();
        // No pull-back: the coil already was the windup, and the frames it costs are frames
        // the Animator spends between the coil and the dash with nothing to show.
        yield return move.Dash(target, Speed(slamSpeed), slamGravity, 1.5f,
                               anticipate: false);
        hitboxes.DisableDiveHitbox();

        SpawnShockwaves();

        yield return move.Hold(buriedTime);
    }

    // The watchdog can kill Act mid-windup, which would leave him stuck in the coil.
    private void OnDisable() {
        if (anim != null) anim.SetStomping(false);
    }

    private void SpawnShockwaves() {
        if (shockwavePrefab == null) return;

        Vector2 origin = new Vector2(move.Position.x, Arena.FloorY + shockwaveYOffset);
        int[] dirs = bothDirections ? new[] { -1, 1 }
                                    : new[] { state.IsFacingRight ? 1 : -1 };

        foreach (int d in dirs) {
            GameObject go = Instantiate(shockwavePrefab, origin, Quaternion.identity);
            SecretaryBirdShockwave wave = go.GetComponent<SecretaryBirdShockwave>();
            if (wave != null) wave.Launch(d, Arena);
        }
    }
}
