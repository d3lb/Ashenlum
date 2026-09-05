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

    public void Hide() {
        if (visual.activeSelf) visual.SetActive(false);
    }
}
