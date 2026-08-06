using System.Collections;
using UnityEngine;

/// <summary>
/// Pure view. Draws where the boss is about to go, then gets out of the way.
///
/// Nothing reads this - no state, no animation, no gameplay check. Delete the LineRenderer
/// and the fight runs identically, it is just unreadable to a human.
///
/// The line is STATIC and BRIEF by design. A line that follows the player as they move is
/// worse than no line: it turns a readable commitment into a thing that keeps changing its
/// mind, so the player learns to ignore it. Show the path, hold, vanish. The boss commits
/// to that path whatever the player does afterwards.
/// </summary>
public class SecretaryBirdTelegraph : MonoBehaviour
{
    [Header("Path line")]
    [SerializeField] private LineRenderer line;
    [SerializeField] private Gradient dangerColor;
    [Tooltip("Used for repositioning, so 'he is moving there' never looks like 'he is striking along here'.")]
    [SerializeField] private Gradient safeColor;
    [SerializeField] private float width = 0.22f;
    [Tooltip("Extends the line past the target so it reads as a trajectory, not a tether.")]
    [SerializeField] private float overshoot = 3f;

    [Header("Timing")]
    [Tooltip("Fraction of the flash spent fading out. 0 = hard cut.")]
    [SerializeField, Range(0f, 0.8f)] private float fadeOutFraction = 0.35f;

    [Header("Ground marker (stomp)")]
    [SerializeField] private SpriteRenderer marker;

    private void Awake()
    {
        if (line == null) line = GetComponent<LineRenderer>();
        if (line != null)
        {
            line.useWorldSpace = true;
            line.positionCount = 2;
        }
        Clear();
    }

    public void Clear()
    {
        if (line != null) line.enabled = false;
        if (marker != null) marker.enabled = false;
    }

    public void Draw(Vector2 from, Vector2 to, bool danger = true)
    {
        if (line == null) return;

        Vector2 delta = to - from;
        Vector2 dir = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.right;

        line.enabled = true;
        line.colorGradient = danger ? dangerColor : safeColor;
        line.startWidth = width;
        line.endWidth = width;
        line.SetPosition(0, from);
        line.SetPosition(1, to + dir * overshoot);
    }

    /// <summary>
    /// Show the path at full strength immediately, hold, fade, gone.
    /// The endpoints are captured once and never updated - that is the whole point.
    /// </summary>
    public IEnumerator Flash(Vector2 from, Vector2 to, float duration, bool danger = true)
    {
        Draw(from, to, danger);

        if (line == null)
        {
            if (duration > 0f) yield return new WaitForSeconds(duration);
            yield break;
        }

        float hold = duration * (1f - fadeOutFraction);
        float fade = duration - hold;

        if (hold > 0f) yield return new WaitForSeconds(hold);

        float t = 0f;
        while (t < fade)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(t / fade);
            line.startWidth = width * k;
            line.endWidth = width * k;
            yield return null;
        }

        Clear();
    }

    /// <summary>Static pulsing decal on the floor. Used for the stomp's impact point.</summary>
    public IEnumerator MarkGround(Vector2 pos, float duration)
    {
        if (marker == null)
        {
            if (duration > 0f) yield return new WaitForSeconds(duration);
            yield break;
        }

        marker.transform.position = pos;
        marker.enabled = true;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            Color c = marker.color;
            c.a = Mathf.Lerp(0.3f, 1f, Mathf.PingPong(t * 9f, 1f));
            marker.color = c;
            yield return null;
        }

        marker.enabled = false;
    }
}
