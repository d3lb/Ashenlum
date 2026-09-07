using TMPro;
using UnityEngine;

// One HUD line of plain text. Newest message wins. Anything with an icon or a description
// is a Notification, not this.
public class Toast : MonoBehaviour {
    public static Toast Instance { get; private set; }

    [SerializeField] private TMP_Text label;
    [SerializeField] private Color defaultColor = Color.white;

    // Seconds of fade at the end of a message. 0 pops it off.
    [SerializeField] private float fadeTime = 0.3f;

    private float hideAt;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (label == null) label = GetComponent<TMP_Text>();

        // enabled, not SetActive: the object stays alive so Update keeps timing out.
        if (label != null) label.enabled = false;
    }

    private void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    // Static so callers never hold a reference, and a scene with no Toast is silent.
    public static void Show(string text, float seconds) {
        if (Instance != null) Instance.Push(text, seconds, Instance.defaultColor);
    }

    public static void Show(string text, float seconds, Color color) {
        if (Instance != null) Instance.Push(text, seconds, color);
    }

    private void Push(string text, float seconds, Color color) {
        if (label == null) return;

        label.text = text;
        label.color = color;
        label.alpha = 1f;
        label.enabled = true;

        hideAt = Time.unscaledTime + seconds;
    }

    // Unscaled: a notice raised as a panel opens should still time out.
    private void Update() {
        if (label == null || !label.enabled) return;

        float remaining = hideAt - Time.unscaledTime;

        if (remaining <= 0f) {
            label.enabled = false;
            return;
        }

        if (fadeTime > 0f) label.alpha = Mathf.Clamp01(remaining / fadeTime);
    }
}
