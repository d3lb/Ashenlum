using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LumenUI : MonoBehaviour {
    [Header("References")]
    [SerializeField] private TMP_Text lumenText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float showDuration = 1.5f;
    [SerializeField] private float fadeOutDuration = 0.4f;

    private Coroutine currentRoutine;

    // Pinned by a menu: it must not fade out just because the amount changed.
    private bool pinned;

    private void Awake() {
        if (PauseManager.Instance != null) PauseManager.Instance.RegisterLumenUI(this);
    }
    private void Start() {
        lumenText.text = GameManager.Instance.CurrentLumens.ToString();

        canvasGroup.alpha = 1f;

        currentRoutine = StartCoroutine(Fade(1f, 0f, fadeOutDuration));
    }

    private void OnEnable() {
        if (GameManager.Instance != null)
            GameManager.Instance.OnLumensChanged += OnLumensChanged;
    }

    private void OnDisable() {
        if (GameManager.Instance != null)
            GameManager.Instance.OnLumensChanged -= OnLumensChanged;
    }

    private void OnLumensChanged(int amount) {
        lumenText.text = amount.ToString();

        if (pinned) return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);
        currentRoutine = null;

        if (!isActiveAndEnabled) return;

        currentRoutine = StartCoroutine(ShowThenHide());
    }

    public IEnumerator ShowThenHide() {
        yield return StartCoroutine(Fade(0f, 1f, fadeInDuration));

        yield return new WaitForSeconds(showDuration);

        yield return StartCoroutine(Fade(1f, 0f, fadeOutDuration));
    }

    public void Hide() {
        pinned = false;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);
        currentRoutine = null;

        // Coroutines cannot start on an inactive object, so snap it hidden.
        if (!isActiveAndEnabled) {
            canvasGroup.alpha = 0f;
            return;
        }

        currentRoutine = StartCoroutine(Fade(1f, 0f, fadeOutDuration));
    }

    public void Show() {
        pinned = true;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);
        currentRoutine = null;

        canvasGroup.alpha = 1f;
    }

    private IEnumerator Fade(float from, float to, float duration) {
        float timer = 0f;
        canvasGroup.alpha = from;
        while (timer < duration) {
            timer += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, timer / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}