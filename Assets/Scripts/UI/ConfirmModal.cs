using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One modal for the whole game. Callers hand it words and a callback; it knows nothing
// about what it is confirming.
//
//   ConfirmModal.Ask("Erase Slot 1?", "This cannot be undone.", "Erase", () => Delete(1));
public class ConfirmModal : MonoBehaviour
{
    public static ConfirmModal Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text   titleText;
    [SerializeField] private TMP_Text   messageText;
    [SerializeField] private Button     confirmButton;
    [SerializeField] private Button     cancelButton;
    [SerializeField] private TMP_Text   confirmLabel;
    [SerializeField] private TMP_Text   cancelLabel;

    [Header("Defaults")]
    [SerializeField] private string defaultConfirmWord = "Confirm";
    [SerializeField] private string cancelWord = "Cancel";

    // Off in the main menu, on in gameplay. TimeManager counts owners, so this stacks
    // safely with the pause menu or the inventory already holding a freeze.
    [SerializeField] private bool freezeTime = true;

    private System.Action onConfirm;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        IsOpen = false;

        if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
        if (cancelButton != null)  cancelButton.onClick.AddListener(Cancel);

        if (panel != null) panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Closing during teardown must not leave the game frozen.
    private void OnDisable()
    {
        if (!IsOpen) return;

        IsOpen = false;
        if (freezeTime) TimeManager.Release(this);
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Cancel();
    }

    public static void Ask(string title, string message, string confirmWord, System.Action onConfirm)
    {
        if (Instance == null)
        {
            // No modal in the scene: doing nothing is safer than silently confirming a
            // destructive action nobody agreed to.
            Debug.LogError("[ConfirmModal] No modal in the scene - the request was dropped.");
            return;
        }

        Instance.Open(title, message, confirmWord, onConfirm);
    }

    private void Open(string title, string message, string confirmWord, System.Action confirmed)
    {
        onConfirm = confirmed;

        if (titleText != null)   titleText.text = title;
        if (messageText != null) messageText.text = message;

        if (confirmLabel != null)
            confirmLabel.text = string.IsNullOrEmpty(confirmWord) ? defaultConfirmWord : confirmWord;

        if (cancelLabel != null) cancelLabel.text = cancelWord;

        if (panel != null) panel.SetActive(true);
        IsOpen = true;

        if (freezeTime) TimeManager.Freeze(this);

        if (confirmButton != null) confirmButton.Select();
    }

    public void Cancel() => Close();

    private void Confirm()
    {
        System.Action callback = onConfirm;

        // Closed before the callback runs, so whatever it does - loading a scene,
        // opening another modal - is not fighting a panel that is still up.
        Close();
        callback?.Invoke();
    }

    private void Close()
    {
        onConfirm = null;
        IsOpen = false;

        if (panel != null) panel.SetActive(false);
        if (freezeTime) TimeManager.Release(this);
    }
}
