using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The "you got it" card. Abilities only - items are too small a moment to stop the game for.
public class AbilityGetUI : MonoBehaviour {
    public static AbilityGetUI Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;

    // The same E press that took the reward is still down this frame, and script order
    // decides who reads it first. Without this the card can open and close instantly.
    [SerializeField] private float inputDelay = 0.35f;

    private float acceptAt;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        IsOpen = false;

        if (panel != null) panel.SetActive(false);
    }

    private void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    // Torn down while open must not leave the game frozen.
    private void OnDisable() {
        if (!IsOpen) return;

        IsOpen = false;
        TimeManager.Release(this);
    }

    // Static so callers never hold a reference, and a scene with no card is silent.
    public static void Show(Sprite sprite, string title, string body) {
        if (Instance != null) Instance.Open(sprite, title, body);
    }

    private void Open(Sprite sprite, string title, string body) {
        if (IsOpen) return;

        if (icon != null) {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }

        if (nameText != null) nameText.text = title;
        if (descriptionText != null) descriptionText.text = body;

        if (panel != null) panel.SetActive(true);

        IsOpen = true;
        acceptAt = Time.unscaledTime + inputDelay;

        TimeManager.Freeze(this);
    }

    private void Update() {
        if (!IsOpen || Time.unscaledTime < acceptAt) return;
        if (!UIInput.AdvancePressed) return;

        Close();
    }

    private void Close() {
        IsOpen = false;

        if (panel != null) panel.SetActive(false);
        TimeManager.Release(this);
    }
}
