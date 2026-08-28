using UnityEngine;
using UnityEngine.UI;

// Full-screen damage overlays, one per stability tier.
//
// Two Images rather than one that swaps its sprite: crossing a threshold would
// otherwise hard-cut the picture while it was already visible, which pops. Fading two
// against each other costs one extra object and looks right.
public class StabilityOverlay : MonoBehaviour
{
    [Header("Overlays")]
    // Both stretched to the full canvas, both with Raycast Target OFF.
    [SerializeField] private Image midOverlay;
    [SerializeField] private Image lowOverlay;

    [Header("Strength")]
    [Range(0f, 1f)] [SerializeField] private float midAlpha = 0.45f;
    [Range(0f, 1f)] [SerializeField] private float lowAlpha = 0.8f;

    [SerializeField] private float fadeSpeed = 4f;

    [Header("Pulse")]
    // Off by default: HealthBar already pulses the vignette, and a second pulse at a
    // different speed reads as flicker rather than a heartbeat. Match its speed if on.
    [SerializeField] private float pulseAmount = 0f;
    [SerializeField] private float pulseSpeed = 4f;

    private PlayerHealth playerHealth;
    private float pulseTimer;

    private void Start() => FindPlayer();

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnSceneReady += FindPlayer;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnSceneReady -= FindPlayer;
    }

    private void FindPlayer() => playerHealth = FindFirstObjectByType<PlayerHealth>();

    private void Update()
    {
        // Between scenes there is no player. Fade out rather than freezing on screen.
        PlayerHealth.StabilityState tier = playerHealth != null
            ? playerHealth.CurrentStabilityState
            : PlayerHealth.StabilityState.High;

        pulseTimer += Time.unscaledDeltaTime * pulseSpeed;

        // Unscaled so the effect keeps breathing while the game is frozen by a panel.
        float pulse = pulseAmount <= 0f
            ? 1f
            : 1f - pulseAmount * (1f - (Mathf.Sin(pulseTimer) + 1f) * 0.5f);

        Fade(midOverlay, tier == PlayerHealth.StabilityState.Mid ? midAlpha * pulse : 0f);
        Fade(lowOverlay, tier == PlayerHealth.StabilityState.Low ? lowAlpha * pulse : 0f);
    }

    private void Fade(Image image, float target)
    {
        if (image == null) return;

        Color c = image.color;
        c.a = Mathf.Lerp(c.a, target, fadeSpeed * Time.unscaledDeltaTime);
        image.color = c;

        // Nothing to draw, so stop it costing a transparent full-screen pass.
        image.enabled = c.a > 0.003f;
    }
}
