using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance;

    [SerializeField] private Image blackPanel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Color c = blackPanel.color;
            c.a = 0f;
            blackPanel.color = c;

        }
        else
        {
            Destroy(gameObject);
        }
    }

    public IEnumerator FadeOut(float duration)
    {
        yield return Fade(0f, 1f, duration);
    }

    public IEnumerator FadeIn(float duration)
    {
        yield return Fade(1f, 0f, duration);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float timer = 0f;

        Color c = blackPanel.color;
        c.a = from;
        blackPanel.color = c;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            c.a = Mathf.Lerp(from, to, timer / duration);
            blackPanel.color = c;

            yield return null;
        }

        c.a = to;
        blackPanel.color = c;
    }

    public void SetBlack()
    {
        Color c = blackPanel.color;
        c.a = 1f;
        blackPanel.color = c;
    }
}