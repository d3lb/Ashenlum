using TMPro;
using UnityEngine;

// Owns only its own visibility and text - Interactable drives it.
public class InteractPrompt : MonoBehaviour {
    [SerializeField] private GameObject visual;
    [SerializeField] private TMP_Text label;
    [SerializeField] private string keyName = "E";

    private void Awake() {
        visual.SetActive(false);
    }

    public void Show(string verb) {
        label.text = $"{keyName} - {verb}";
        if (!visual.activeSelf) visual.SetActive(true);
    }

    // No key prefix: a reason, not something to press.
    public void ShowMessage(string text) {
        label.text = text;
        if (!visual.activeSelf) visual.SetActive(true);
    }

    public void Hide() {
        if (visual.activeSelf) visual.SetActive(false);
    }
}
