using System.Collections;
using UnityEngine;

public class SecretaryBirdTelegraph : MonoBehaviour
{
    [Header("Path line")]
    [SerializeField] private LineRenderer line;
    [SerializeField] private Gradient color;

    [SerializeField] private float width = 0.22f;
    [SerializeField] private float overshoot = 3f;

    [Header("Timing")]
    [SerializeField, Range(0f, 0.8f)] private float fadeOutFraction = 0.35f;

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
    }

    public void Draw(Vector2 from, Vector2 to, bool danger = true)
    {
        if (line == null) return;

        Vector2 delta = to - from;
        Vector2 dir = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.right;

        line.enabled = true;
        line.colorGradient = color;
        line.startWidth = width;
        line.endWidth = width;
        line.SetPosition(0, from);
        line.SetPosition(1, to + dir * overshoot);
    }

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
}
