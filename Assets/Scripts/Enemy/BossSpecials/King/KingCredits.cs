using System.Collections;
using UnityEngine;

// Fade to black, hold on a credits card, fade back to the arena.
//
// Skipping matches the dialogue rules: a press ends the current step and nothing else.
// The final fade in is not skippable, so the game never snaps straight from a black
// screen back into play.
public class KingCredits : MonoBehaviour
{
    [Header("Card")]
    // Whatever the credits text lives on. Written in the editor, not here.
    [SerializeField] private GameObject panel;

    [Header("Timing")]
    [SerializeField] private float fadeOutTime = 1.5f;

    // The names come up out of the black rather than appearing on it.
    [SerializeField] private float textFadeIn = 1f;
    [SerializeField] private float holdTime = 6f;
    [SerializeField] private float textFadeOut = 0.8f;

    // A beat of empty black after the names go and before the room comes back.
    [SerializeField] private float blackBeat = 0.5f;
    [SerializeField] private float fadeInTime = 1.5f;

    // Stops the press that killed him from also skipping the first fade.
    [SerializeField] private float inputDelay = 0.35f;

    private CanvasGroup group;

    private void Awake()
    {
        if (panel == null) return;

        // Added rather than required, so there is nothing extra to wire by hand.
        group = panel.GetComponent<CanvasGroup>();
        if (group == null) group = panel.AddComponent<CanvasGroup>();

        group.alpha = 0f;
        panel.SetActive(false);
    }

    private static bool Pressed => UIInput.AdvancePressed;

    public IEnumerator Play()
    {
        // Frozen for the whole card, so nothing keeps fighting behind the black.
        // The flag is separate: Update still runs at timeScale 0, so without it the
        // click that skips a step also swings the sword.
        TimeManager.Freeze(this);
        UIState.CutsceneActive = true;

        yield return SkippableFade(fadeOutTime);

        if (panel != null) panel.SetActive(true);

        yield return FadeGroup(0f, 1f, textFadeIn, true);
        yield return SkippableWait(holdTime);

        // From here on nothing skips. The way out should feel decided, not rushed.
        yield return FadeGroup(1f, 0f, textFadeOut, false);

        if (panel != null) panel.SetActive(false);

        if (blackBeat > 0f) yield return WaitUnscaled(blackBeat);

        if (SceneFader.Instance != null)
            yield return SceneFader.Instance.FadeIn(fadeInTime);

        UIState.CutsceneActive = false;
        TimeManager.Release(this);
    }

    // Runs the fade and cuts it short on a press, landing on full black either way.
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

        // Always lands on the target, whether it ran out or was skipped.
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

    // Safety net: torn down mid-card must not leave the game frozen or black.
    private void OnDisable()
    {
        UIState.CutsceneActive = false;
        TimeManager.Release(this);
    }
}
