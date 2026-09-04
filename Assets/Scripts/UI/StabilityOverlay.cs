using UnityEngine;
using UnityEngine.UI;

// One overlay per stability tier. Two Images, so crossing a threshold cross-fades.
public class StabilityOverlay : MonoBehaviour
{
    [Header("Overlays")]
    // Full canvas, Raycast Target OFF.
    [SerializeField] private Image midOverlay;
    [SerializeField] private Image lowOverlay;

    [Header("Strength")]
    [Range(0f, 1f)] [SerializeField] private float midAlpha = 0.45f;
    [Range(0f, 1f)] [SerializeField] private float lowAlpha = 0.8f;

    [SerializeField] private float fadeSpeed = 4f;

    [Header("Pulse")]
    // Off by default: HealthBar already pulses, and two speeds read as flicker.
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
        PlayerHealth.StabilityState tier = playerHealth != null
            ? playerHealth.CurrentStabilityState
            : PlayerHealth.StabilityState.High;

        pulseTimer += Time.unscaledDeltaTime * pulseSpeed;

        // Unscaled, so it keeps breathing while a panel freezes the game.
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

        // Nothing to draw, so skip the full-screen pass.
        image.enabled = c.a > 0.003f;
    }
}
