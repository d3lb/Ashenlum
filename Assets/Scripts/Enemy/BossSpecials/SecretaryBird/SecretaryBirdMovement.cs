using System.Collections;
using UnityEngine;

/// <summary>
/// Movement primitives only. Knows nothing about attacks, hitboxes, phases or what comes next.
///
/// There is ONE way this boss travels: Dash(). Attacking, repositioning, rising for a stomp -
/// all of it is the same explosive blink. That is deliberate. A boss that glides to its perch
/// and then dashes at you reads as two different creatures.
///
/// IMPORTANT: every method here is an IEnumerator meant to be consumed with
/// `yield return move.Xxx(...)` and NEVER with StartCoroutine, so the brain's watchdog can
/// abort an entire attack - movement included - with a single StopCoroutine.
/// </summary>
public class SecretaryBirdMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SecretaryBirdState state;
    [SerializeField] private SecretaryBirdArena arena;

    [Header("Dash feel")]
    [Tooltip("Speed multiplier across the dash (0 = launch, 1 = arrival). The whole 'blink' " +
             "read comes from this being high and front-loaded - flat = a slide, not a strike.")]
    [SerializeField] private AnimationCurve dashSpeedCurve = new AnimationCurve(
        new Keyframe(0f, 1.7f),
        new Keyframe(0.12f, 1.25f),
        new Keyframe(0.7f, 0.95f),
        new Keyframe(1f, 0.8f));

    [Tooltip("Tiny counter-movement before the launch. This is the single biggest reason " +
             "a fast move reads as explosive instead of as teleporting. Keep it under 0.08s.")]
    [SerializeField] private float anticipationTime = 0.06f;
    [SerializeField] private float anticipationDistance = 0.45f;

    [Header("Impact feedback")]
    [Tooltip("Hitstop when the dash slams into geometry. Sells the speed more than the speed does.")]
    [SerializeField] private float impactHitStop = 0.045f;
    [SerializeField] private float impactShakeDuration = 0.12f;
    [SerializeField] private float impactShakeAmplitude = 2.5f;
    [SerializeField] private float impactShakeFrequency = 3f;

    [Header("Impact detection")]
    [Tooltip("Layers that can end a dash. Ground/walls only - NOT the player.")]
    [SerializeField] private LayerMask impactLayers;

    [Tooltip("How head-on a contact must be to count. 0 = ANY touch ends the dash, which means " +
             "the floor under a low dash and the wall you launched from both stop it instantly.")]
    [SerializeField, Range(0f, 0.95f)] private float impactFacing = 0.35f;

    [Tooltip("Contacts during the first moments of a dash are ignored, so kicking off a wall " +
             "you are already resting against cannot end the dash on frame one.")]
    [SerializeField] private float impactGrace = 0.06f;

    [Header("Defaults")]
    [SerializeField] private float defaultGravity = 3f;

    [Header("Debug")]
    [SerializeField] private bool logDashEnd;

    private bool impacted;
    private bool dashing;
    private Vector2 dashDir;
    private float impactOpensAt;

    public SecretaryBirdArena Arena => arena;
    public Rigidbody2D Body => rb;
    public Vector2 Position => rb.position;
    public float DefaultGravity => defaultGravity;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        state = GetComponent<SecretaryBirdState>();
    }

    /// <summary>
    /// Called by SecretaryBirdCollision on Enter and Stay.
    ///
    /// The direction test is the important part. The boss perches ON a wall, so it is in
    /// continuous contact the instant a dash begins - an unfiltered "did I touch anything"
    /// check ends every dash immediately. A contact only counts if its surface normal
    /// opposes the direction of travel.
    /// </summary>
    public void ReportImpact(Collision2D c)
    {
        if (!dashing) return;
        if (Time.time < impactOpensAt) return;
        if (((1 << c.gameObject.layer) & impactLayers) == 0) return;

        for (int i = 0; i < c.contactCount; i++)
        {
            if (Vector2.Dot(c.GetContact(i).normal, dashDir) <= -impactFacing)
            {
                impacted = true;
                if (logDashEnd)
                    Debug.Log($"[SecretaryBird] dash ended on '{c.gameObject.name}'", c.gameObject);
                return;
            }
        }
    }

    public void Stop()
    {
        if (rb == null) return;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    public void SetGravity(float g) => rb.gravityScale = g;
    public void ResetGravity()      => rb.gravityScale = defaultGravity;

    public void ClampInsideArena()
    {
        if (arena != null) rb.position = arena.Clamp(rb.position);
    }

    /// <summary>
    /// The one and only way this boss moves. Anticipate, blink, slam.
    ///
    /// Ends on arrival, on a head-on impact, or on timeout - three independent exits, so it
    /// cannot hang the way a single WaitUntil could.
    ///
    /// arcGravity > 0 gives the dive its weight. Gravity is integrated by hand here rather
    /// than left to the rigidbody, because the speed curve has to stay authoritative over
    /// the along-dash component.
    /// </summary>
    public IEnumerator Dash(Vector2 target, float speed, float arcGravity = 0f,
                            float maxTime = 2f, float arriveDist = 0.35f,
                            bool anticipate = true, bool feedbackOnImpact = true)
    {
        var wait = new WaitForFixedUpdate();

        Vector2 start = rb.position;
        Vector2 dir = (target - start).normalized;
        if (dir == Vector2.zero) dir = state.IsFacingRight ? Vector2.right : Vector2.left;

        state.SetFacing(dir.x >= 0f);
        rb.gravityScale = 0f;

        // --- Anticipation: a short pull-back AGAINST the dash direction. ---
        if (anticipate && anticipationTime > 0f && anticipationDistance > 0f)
        {
            Vector2 back = start - dir * anticipationDistance;
            float a = 0f;
            while (a < anticipationTime)
            {
                a += Time.fixedDeltaTime;
                rb.MovePosition(Vector2.Lerp(start, back, Mathf.Clamp01(a / anticipationTime)));
                yield return wait;
            }
        }

        // --- Launch ---
        impacted = false;
        dashing = true;
        dashDir = dir;
        impactOpensAt = Time.time + impactGrace;

        float totalDist = Mathf.Max(0.01f, Vector2.Distance(rb.position, target));
        float gravAccum = 0f;
        float deadline = Time.time + maxTime;

        while (!impacted && Time.time < deadline)
        {
            float remaining = Vector2.Dot(target - rb.position, dir);
            if (remaining <= arriveDist) break;

            float progress = Mathf.Clamp01(1f - (remaining / totalDist));
            float mult = dashSpeedCurve != null ? dashSpeedCurve.Evaluate(progress) : 1f;

            gravAccum += Physics2D.gravity.y * arcGravity * Time.fixedDeltaTime;
            rb.linearVelocity = dir * (speed * mult) + Vector2.up * gravAccum;

            yield return wait;
        }

        dashing = false;
        Stop();

        if (feedbackOnImpact && impacted) ImpactFeedback();
    }

    private void ImpactFeedback()
    {
        if (impactHitStop > 0f && TimeManager.Instance != null)
            TimeManager.Instance.HitStop(impactHitStop);

        if (impactShakeDuration > 0f && CameraShake.Instance != null)
            CameraShake.Instance.Shake(impactShakeDuration, impactShakeAmplitude, impactShakeFrequency);
    }

    /// <summary>
    /// Non-explosive glide. Only for things that are meant to look weightless - it is NOT
    /// used for perching any more, because perching is a dash like everything else.
    /// </summary>
    public IEnumerator Glide(Vector2 target, float speed, float arriveDist = 0.1f, float maxTime = 3f)
    {
        state.CurrentState = SecretaryBirdState.BossStateType.Reposition;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        var wait = new WaitForFixedUpdate();
        float deadline = Time.time + maxTime;

        while (Vector2.Distance(rb.position, target) > arriveDist && Time.time < deadline)
        {
            rb.MovePosition(Vector2.MoveTowards(rb.position, target, speed * Time.fixedDeltaTime));
            yield return wait;
        }

        yield return wait;
    }

    /// <summary>Hover in place. Gravity off so the beat reads as a deliberate pause.</summary>
    public IEnumerator Hold(float seconds)
    {
        Stop();
        rb.gravityScale = 0f;
        if (seconds > 0f) yield return new WaitForSeconds(seconds);
    }
}
