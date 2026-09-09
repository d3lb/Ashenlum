using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Goes on the player's Spot Light. Drops the light almost to nothing and lets it swell back,
// so a bad hit reads as "you nearly went out" rather than a generic flash.
public class PlayerLightGutter : MonoBehaviour {
    public static PlayerLightGutter Instance { get; private set; }

    [SerializeField] private Light2D spotLight;

    // Fraction of the light's normal intensity to fall to.
    [SerializeField, Range(0f, 1f)] private float dipTo = 0.08f;

    [SerializeField] private float outTime = 0.06f;
    [SerializeField] private float backTime = 0.6f;

    private float baseIntensity;
    private Coroutine running;

    private void Awake() {
        Instance = this;

        if (spotLight == null) spotLight = GetComponent<Light2D>();
        if (spotLight != null) baseIntensity = spotLight.intensity;
    }

    private void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    // Torn down mid-gutter would leave the player in the dark permanently.
    private void OnDisable() {
        running = null;
        if (spotLight != null) spotLight.intensity = baseIntensity;
    }

    public static void Gutter() {
        if (Instance != null) Instance.Play();
    }

    private void Play() {
        if (spotLight == null) return;

        if (running != null) StopCoroutine(running);
        running = StartCoroutine(Run());
    }

    // Unscaled throughout: this is meant to play during the freeze the spike holds.
    private IEnumerator Run() {
        float from = spotLight.intensity;
        float low = baseIntensity * dipTo;

        float t = 0f;
        while (t < outTime) {
            t += Time.unscaledDeltaTime;
            spotLight.intensity = Mathf.Lerp(from, low, t / outTime);
            yield return null;
        }

        t = 0f;
        while (t < backTime) {
            t += Time.unscaledDeltaTime;
            spotLight.intensity = Mathf.Lerp(low, baseIntensity, t / backTime);
            yield return null;
        }

        spotLight.intensity = baseIntensity;
        running = null;
    }
}
