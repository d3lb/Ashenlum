using UnityEngine;

// The shadow that swallows the room. Purely decorative - the reset already happened
// behind it. All it reports is when it is done playing.
public class RestWave : MonoBehaviour
{
    [Header("Shape")]
    [SerializeField] private Transform circle;
    [SerializeField] private SpriteRenderer fade;

    [Header("Growth")]
    [SerializeField] private float maxRadius = 30f;
    [SerializeField] private float growTime = 1.2f;
    [SerializeField] private AnimationCurve growth = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Fade")]
    [SerializeField] private float holdTime = 0.5f;
    [SerializeField] private float fadeTime = 0.4f;

    private float elapsed;

    public bool Finished => elapsed >= growTime + holdTime + fadeTime;

    private void Awake()
    {
        if (circle == null) circle = transform;
    }

    private void Update()
    {
        // Unscaled: resting freezes the game, the wave still has to play.
        elapsed += Time.unscaledDeltaTime;

        float g = growTime <= 0f ? 1f : Mathf.Clamp01(elapsed / growTime);
        float diameter = maxRadius * 2f * growth.Evaluate(g);
        circle.localScale = new Vector3(diameter, diameter, 1f);

        if (fade == null) return;

        float sinceHold = elapsed - growTime - holdTime;

        float alpha;
        if (sinceHold <= 0f)     alpha = 1f;
        else if (fadeTime <= 0f) alpha = 0f;
        else                     alpha = 1f - Mathf.Clamp01(sinceHold / fadeTime);

        Color c = fade.color;
        c.a = alpha;
        fade.color = c;
    }
}
