using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LumenUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text lumenText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float showDuration = 1.5f;
    [SerializeField] private float fadeOutDuration = 0.4f;

    private Coroutine currentRoutine;

    private void Start()
    {
        canvasGroup.alpha = 1f;
        StartCoroutine(Hide());
        GameManager.Instance.OnLumensChanged += OnLumensChanged;
    }

    private void OnLumensChanged(int amount)
    {
        lumenText.text = amount.ToString();

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(ShowThenHide());
    }

    private IEnumerator ShowThenHide()
    {
        // Fade in
        yield return StartCoroutine(Fade(0f, 1f, fadeInDuration));

        // Hold
        yield return new WaitForSeconds(showDuration);

        // Fade out
        yield return StartCoroutine(Fade(1f, 0f, fadeOutDuration));
    }

    private IEnumerator Hide()
    {
        // Hold
        yield return new WaitForSeconds(showDuration);

        // Fade out
        yield return StartCoroutine(Fade(1f, 0f, fadeOutDuration));
    }

    private IEnumerator Show()
    {
        // Fade in
        yield return StartCoroutine(Fade(0f, 1f, fadeInDuration));
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float timer = 0f;
        canvasGroup.alpha = from;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, timer / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}