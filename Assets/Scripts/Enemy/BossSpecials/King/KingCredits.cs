using System.Collections;
using UnityEngine;

// A press ends the current step only. The way out does not skip.
public class KingCredits : MonoBehaviour
{
    [Header("Card")]
    [SerializeField] private GameObject panel;

    [Header("Timing")]
    [SerializeField] private float fadeOutTime = 1.5f;

    [SerializeField] private float textFadeIn = 1f;
    [SerializeField] private float holdTime = 6f;
    [SerializeField] private float textFadeOut = 0.8f;

    // Empty black between the names leaving and the room returning.
    [SerializeField] private float blackBeat = 0.5f;
    [SerializeField] private float fadeInTime = 1.5f;

    // Or the killing blow's press also skips the first fade.
    [SerializeField] private float inputDelay = 0.35f;

    private CanvasGroup group;

    private void Awake()
    {
        if (panel == null) return;

        group = panel.GetComponent<CanvasGroup>();
        if (group == null) group = panel.AddComponent<CanvasGroup>();

        group.alpha = 0f;
        panel.SetActive(false);
    }

    private static bool Pressed => UIInput.AdvancePressed;

    public IEnumerator Play()
    {
        // Freeze stops the world; the flag stops input, which Update still reads at timeScale 0.
        TimeManager.Freeze(this);
        UIState.CutsceneActive = true;

        yield return SkippableFade(fadeOutTime);

        if (panel != null) panel.SetActive(true);

        yield return FadeGroup(0f, 1f, textFadeIn, true);
        yield return SkippableWait(holdTime);

        yield return FadeGroup(1f, 0f, textFadeOut, false);

        if (panel != null) panel.SetActive(false);

        if (blackBeat > 0f) yield return WaitUnscaled(blackBeat);

        if (SceneFader.Instance != null)
            yield return SceneFader.Instance.FadeIn(fadeInTime);

        UIState.CutsceneActive = false;
        TimeManager.Release(this);
    }

    // Cut short on a press, but always lands on full black.
    private IEnumerator SkippableFade(float duration)
    {
        if (SceneFader.Instance == null) yield break;

        Coroutine fade = StartCoroutine(SceneFader.Instance.FadeOut(duration));

        float t = 0f;
        bool skipped = false;

        while (t < duration && !skipped)
        {
            t += Time.unscaledDeltaTime;
            if (t > inputDelay && Pressed) skipped = true;
            yield return null;
        }

        if (skipped) StopCoroutine(fade);

        SceneFader.Instance.SetBlack();
    }

    private IEnumerator FadeGroup(float from, float to, float duration, bool skippable)
    {
        if (group == null) yield break;

        group.alpha = from;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;

            if (skippable && t > inputDelay && Pressed) break;

            group.alpha = Mathf.Lerp(from, to, duration <= 0f ? 1f : t / duration);
            yield return null;
        }

        // Lands on the target whether it ran out or was skipped.
        group.alpha = to;
    }

    private static IEnumerator WaitUnscaled(float duration)
    {
        float t = 0f;
        while (t < duration) { t += Time.unscaledDeltaTime; yield return null; }
    }

    private IEnumerator SkippableWait(float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            if (t > inputDelay && Pressed) yield break;
            yield return null;
        }
    }

    // Torn down mid-card must not leave the game frozen.
    private void OnDisable()
    {
        UIState.CutsceneActive = false;
        TimeManager.Release(this);
    }
}
