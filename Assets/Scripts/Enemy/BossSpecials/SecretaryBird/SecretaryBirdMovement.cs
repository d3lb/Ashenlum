using System.Collections;
using UnityEngine;

public class SecretaryBirdMovement : MonoBehaviour {
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SecretaryBirdState state;
    [SerializeField] private SecretaryBirdArena arena;

    [Header("Dash feel")]
    [SerializeField] private AnimationCurve dashSpeedCurve = new AnimationCurve(
        new Keyframe(0f, 1.7f),
        new Keyframe(0.12f, 1.25f),
        new Keyframe(0.7f, 0.95f),
        new Keyframe(1f, 0.8f));

    [SerializeField] private float anticipationTime = 0.06f;
    [SerializeField] private float anticipationDistance = 0.45f;

    [Header("Impact feedback")]
    [SerializeField] private float impactHitStop = 0.045f;
    [SerializeField] private float impactShakeDuration = 0.12f;
    [SerializeField] private float impactShakeAmplitude = 2.5f;
    [SerializeField] private float impactShakeFrequency = 3f;

    [Header("Impact detection")]
    [SerializeField] private LayerMask impactLayers;

    [SerializeField, Range(0f, 0.95f)] private float impactFacing = 0.35f;

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

    private void Reset() {
        rb = GetComponent<Rigidbody2D>();
        state = GetComponent<SecretaryBirdState>();
    }

    // He perches ON a wall, so an unfiltered contact check ends every dash on frame one.
    public void ReportImpact(Collision2D c) {
        if (!dashing) return;
        if (Time.time < impactOpensAt) return;
        if (((1 << c.gameObject.layer) & impactLayers) == 0) return;

        for (int i = 0; i < c.contactCount; i++) {
            if (Vector2.Dot(c.GetContact(i).normal, dashDir) <= -impactFacing) {
                impacted = true;
                if (logDashEnd)
                    Debug.Log($"[SecretaryBird] dash ended on '{c.gameObject.name}'", c.gameObject);
                return;
            }
        }
    }

    public void Stop() {
        if (rb == null) return;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    public void SetGravity(float g) => rb.gravityScale = g;
    public void ResetGravity()      => rb.gravityScale = defaultGravity;

    public void ClampInsideArena() {
        if (arena != null) rb.position = arena.Clamp(rb.position);
    }

    public IEnumerator Dash(Vector2 target, float speed, float arcGravity = 0f,
                            float maxTime = 2f, float arriveDist = 0.35f,
                            bool anticipate = true, bool feedbackOnImpact = true) {
        var wait = new WaitForFixedUpdate();

        Vector2 start = rb.position;

        // Already there - without this the pull-back fires in an arbitrary direction.
        if (Vector2.Distance(start, target) <= arriveDist) {
            Stop();
            rb.gravityScale = 0f;
            yield return wait;
            yield break;
        }

        Vector2 dir = (target - start).normalized;

        state.SetFacing(dir.x >= 0f);
        rb.gravityScale = 0f;

        //  Anticipation: a short pull-back AGAINST the dash direction. 
        if (anticipate && anticipationTime > 0f && anticipationDistance > 0f) {
            Vector2 back = start - dir * anticipationDistance;
            float a = 0f;
            while (a < anticipationTime) {
                a += Time.fixedDeltaTime;
                rb.MovePosition(Vector2.Lerp(start, back, Mathf.Clamp01(a / anticipationTime)));
                yield return wait;
            }
        }

        //  Launch 
        impacted = false;
        dashing = true;
        dashDir = dir;
        impactOpensAt = Time.time + impactGrace;

        float totalDist = Mathf.Max(0.01f, Vector2.Distance(rb.position, target));
        float gravAccum = 0f;
        float deadline = Time.time + maxTime;

        while (!impacted && Time.time < deadline) {
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

    private void ImpactFeedback() {
        if (impactHitStop > 0f && TimeManager.Instance != null)
            TimeManager.Instance.HitStop(impactHitStop);

        if (impactShakeDuration > 0f && CameraShakeManager.Instance != null)
            CameraShakeManager.Instance.Shake(impactShakeDuration, impactShakeAmplitude, impactShakeFrequency);
    }

    public IEnumerator Glide(Vector2 target, float speed, float arriveDist = 0.1f, float maxTime = 3f) {
        state.CurrentState = SecretaryBirdState.BossStateType.Reposition;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        var wait = new WaitForFixedUpdate();
        float deadline = Time.time + maxTime;

        while (Vector2.Distance(rb.position, target) > arriveDist && Time.time < deadline) {
            rb.MovePosition(Vector2.MoveTowards(rb.position, target, speed * Time.fixedDeltaTime));
            yield return wait;
        }

        yield return wait;
    }

    public IEnumerator Hold(float seconds) {
        Stop();
        rb.gravityScale = 0f;
        if (seconds > 0f) yield return new WaitForSeconds(seconds);
    }
}
