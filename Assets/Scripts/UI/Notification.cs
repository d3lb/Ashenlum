using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Icon, name and description. Raised by the run profile whenever something enters the
// inventory - so it fires no matter who granted it - and reused as the inventory's own
// detail view while hovering an entry.
public class Notification : MonoBehaviour {
    public static Notification Instance { get; private set; }

    [Header("References")]
    // Toggled, so the script itself stays alive on an always-active parent.
    [SerializeField] private GameObject panel;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private CanvasGroup group;

    [Header("Timing")]
    [SerializeField] private float showTime = 3f;
    [SerializeField] private float fadeTime = 0.5f;

    private float hideAt;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Clear();
    }

    private void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    // A pickup. Times out on its own.
    public static void Show(Sprite sprite, string title, string body) {
        if (Instance != null) Instance.Fill(sprite, title, body, Instance.showTime);
    }

    // Hovering an inventory entry. Stays up until Release.
    public static void Hold(Sprite sprite, string title, string body) {
        if (Instance != null) Instance.Fill(sprite, title, body, float.PositiveInfinity);
    }

    public static void Release() {
        if (Instance != null) Instance.Clear();
    }

    private void Fill(Sprite sprite, string title, string body, float seconds) {
        if (icon != null) {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }

        if (nameText != null) nameText.text = title;
        if (descriptionText != null) descriptionText.text = body;

        if (group != null) group.alpha = 1f;
        if (panel != null) panel.SetActive(true);

        hideAt = seconds;
        if (!float.IsPositiveInfinity(seconds)) hideAt = Time.unscaledTime + seconds;
    }

    private void Clear() {
        hideAt = 0f;
        if (panel != null) panel.SetActive(false);
    }

    // Unscaled: the inventory freezes time, and a pickup notice must still time out.
    private void Update() {
        if (panel == null || !panel.activeSelf) return;

        // Held for a hover - no countdown at all.
        if (float.IsPositiveInfinity(hideAt)) return;

        float remaining = hideAt - Time.unscaledTime;

        if (remaining <= 0f) {
            Clear();
            return;
        }

        if (group != null && fadeTime > 0f)
            group.alpha = Mathf.Clamp01(remaining / fadeTime);
    }
}
