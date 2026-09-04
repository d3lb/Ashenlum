using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One row in the travel list.
public class TravelEntryUI : MonoBehaviour {
    [SerializeField] private TMP_Text label;
    [SerializeField] private Button   button;

    private System.Action onClick;

    private void Awake() {
        if (button != null) button.onClick.AddListener(() => onClick?.Invoke());
    }

    public void Bind(string text, bool isHere, System.Action click) {
        if (label != null) label.text = isHere ? $"{text}  (here)" : text;

        onClick = click;

        // Listed, but travelling to where you already are is pointless.
        if (button != null) button.interactable = !isHere && click != null;
    }
}
