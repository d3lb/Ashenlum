using System.Collections;
using UnityEngine;

/// <summary>
/// The secretary bird's real-world kill move, and the fight's rhythm-breaker.
///
/// Blinks to a point directly above the player, marks the floor, slams, and sends
/// shockwaves along the ground. It demands TWO answers back to back - step off the marker,
/// then jump the wave - which no dash in the moveset does.
///
/// The overhead position is committed to at the moment he arrives; he does not hover and
/// follow. Standing still under him is punished by the marker, not by an unwinnable chase.
///
/// His legs stay buried afterwards: the fight's longest punish window and, if you assign
/// pogoTarget, the only moment the player can pogo the boss.
/// </summary>
public class SecretaryBirdStomp : SecretaryBirdAttack
{
    [Header("Rise")]
    [SerializeField, Range(0f, 1f)] private float riseHeight = 0.9f;
    [SerializeField] private float hangTime = 0.12f;

    [Header("Telegraph")]
    [Tooltip("Line down + pulsing floor marker. This is the dodge window.")]
    [SerializeField] private float telegraphTime = 0.25f;
    [SerializeField] private float markTime = 0.28f;

    [Header("Slam")]
    [SerializeField] private float slamSpeed = 62f;
    [SerializeField] private float slamGravity = 2.5f;

    [Header("Shockwave")]
    [SerializeField] private GameObject shockwavePrefab;
    [SerializeField] private float shockwaveYOffset = 0.4f;
    [SerializeField] private bool bothDirections = true;

    [Header("Pogo window")]
    [Tooltip("A child object with your PogoTarget component. Enabled only while buried.")]
    [SerializeField] private GameObject pogoTarget;
    [SerializeField] private float buriedTime = 0.9f;

    public override string DisplayName => "Stomp";

    protected override void Awake()
    {
        base.Awake();
        if (pogoTarget != null) pogoTarget.SetActive(false);
    }

    public override IEnumerator Act(Transform player)
    {
        // Blink to a point above the player. Vertical silhouette = unmistakable.
        Vector2 above = new Vector2(
            Arena.ClampX(player.position.x),
            Mathf.Lerp(Arena.FloorY, Arena.CeilY, riseHeight));

        yield return BlinkTo(above);
        state.FaceTowards(player.position.x);
        yield return move.Hold(hangTime);

        // Commit straight down from wherever he ended up. Static, no re-aiming.
        Vector2 target = new Vector2(move.Position.x, Arena.FloorY);
        yield return ShowPath(target, telegraphTime);

        if (telegraph != null)
            yield return telegraph.MarkGround(target, markTime);
        else
            yield return move.Hold(markTime);

        state.CurrentState = SecretaryBirdState.BossStateType.Attacking;
        hitboxes.EnableDiveHitbox();
        yield return move.Dash(target, slamSpeed, slamGravity, 1.5f);
        hitboxes.DisableDiveHitbox();

        SpawnShockwaves();

        // Buried. The reward for reading it.
        if (pogoTarget != null) pogoTarget.SetActive(true);
        yield return move.Hold(buriedTime);
        if (pogoTarget != null) pogoTarget.SetActive(false);
    }

    private void SpawnShockwaves()
    {
        if (shockwavePrefab == null) return;

        Vector2 origin = new Vector2(move.Position.x, Arena.FloorY + shockwaveYOffset);
        int[] dirs = bothDirections ? new[] { -1, 1 }
                                    : new[] { state.IsFacingRight ? 1 : -1 };

        foreach (int d in dirs)
        {
            GameObject go = Instantiate(shockwavePrefab, origin, Quaternion.identity);
            SecretaryBirdShockwave wave = go.GetComponent<SecretaryBirdShockwave>();
            if (wave != null) wave.Launch(d, Arena);
        }
    }
}
