using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider easeHealthSlider;
    [SerializeField] private RectTransform barTransform;
    [SerializeField] private Image fillImage;      // the health bar fill graphic
    [SerializeField] private Volume globalVolume;  // drag your Global Volume here
    [SerializeField] private float widthPerHP = 2f;

    [Header("Pulse Shake")]
    [SerializeField] private float midShakeAmplitude = 0.3f;
    [SerializeField] private float lowShakeAmplitude = 0.6f;
    [SerializeField] private float shakeDuration = 0.15f;
    [SerializeField] private float shakeFrequency = 2f;

    [Header("Colors")]
    [SerializeField] private Color highColor = Color.white;
    [SerializeField] private Color midColor = new Color(0.7f, 0.4f, 0.9f);
    [SerializeField] private Color lowColor = new Color(0.6f, 0.1f, 0.8f);

    [Header("Vignette")]
    [SerializeField] private Color vignetteColor = new Color(0.5f, 0f, 0.8f);
    [SerializeField] private float midPulseIntensity = 0.15f;
    [SerializeField] private float lowPulseIntensity = 0.35f;
    [SerializeField] private float midPulseSpeed = 2f;
    [SerializeField] private float lowPulseSpeed = 4f;

    [Header("Settings")]
    [SerializeField] private float lerpSpeed = 5f;
    [SerializeField] private float colorLerpSpeed = 6f;

    private PlayerHealth playerHealth;
    private Vignette vignette;
    private float pulseTimer;
    private bool pulseWasRising;

    private void Start()
    {
        FindPlayer();
        if (globalVolume != null)
            globalVolume.profile.TryGet(out vignette);
        if (vignette != null)
            vignette.color.value = vignetteColor;
    }

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

    private void FindPlayer()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            float percent = (float)playerHealth.CurrentHP / playerHealth.MaxHP;
            healthSlider.value = percent;
            easeHealthSlider.value = percent;
        }
    }

    private void Update()
    {
        if (playerHealth == null) return;

        float percent = (float)playerHealth.CurrentHP / playerHealth.MaxHP;
        healthSlider.value = percent;
        easeHealthSlider.value = Mathf.Lerp(easeHealthSlider.value, percent, lerpSpeed * Time.deltaTime);

        float width = playerHealth.MaxHP * widthPerHP;
        barTransform.sizeDelta = new Vector2(width, barTransform.sizeDelta.y);

        UpdateStabilityVisuals();
    }

    private void UpdateStabilityVisuals()
    {
        PlayerHealth.StabilityState stability = playerHealth.CurrentStabilityState;

        Color targetColor;
        float pulseIntensity;
        float pulseSpeed;

        switch (stability)
        {
            case PlayerHealth.StabilityState.High:
                targetColor = highColor;
                pulseIntensity = 0f;
                pulseSpeed = 0f;
                break;
            case PlayerHealth.StabilityState.Mid:
                targetColor = midColor;
                pulseIntensity = midPulseIntensity;
                pulseSpeed = midPulseSpeed;
                break;
            default:
                targetColor = lowColor;
                pulseIntensity = lowPulseIntensity;
                pulseSpeed = lowPulseSpeed;
                break;
        }

        // Bar color
        if (fillImage != null)
            fillImage.color = Color.Lerp(fillImage.color, targetColor, colorLerpSpeed * Time.deltaTime);

        // Vignette pulse
        if (vignette == null) return;

        if (pulseIntensity <= 0f)
        {
            vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, 0f, colorLerpSpeed * Time.deltaTime);
            pulseTimer = 0f;
            return;
        }

        pulseTimer += Time.deltaTime * pulseSpeed;
        float pulse = (Mathf.Sin(pulseTimer) + 1f) * 0.5f;
        float target = pulseIntensity * pulse;
        vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, target, colorLerpSpeed * Time.deltaTime);

        // Detect peak of the pulse to fire a shake
        bool rising = Mathf.Cos(pulseTimer) > 0f;
        if (pulseWasRising && !rising)
        {
            float amp = stability == PlayerHealth.StabilityState.Low ? lowShakeAmplitude : midShakeAmplitude;
            if (CameraShakeManager.Instance != null)
                CameraShakeManager.Instance.Shake(shakeDuration, amp, shakeFrequency);
        }
        pulseWasRising = rising;
    }
}