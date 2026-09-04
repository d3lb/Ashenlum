using TMPro;
using UnityEngine;
using UnityEngine.UI;

// InventoryUI hands it a sprite and a click action; it knows nothing else.
public class InventoryEntryUI : MonoBehaviour {
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Button button;

    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color dimmedColor = new Color(1f, 1f, 1f, 0.3f);

    private System.Action onClick;

    private void Awake() {
        button.onClick.AddListener(() => onClick?.Invoke());
    }

    public void Bind(Sprite sprite, int count, bool dimmed, System.Action click) {
        icon.sprite = sprite;
        icon.enabled = sprite != null;
        icon.color = dimmed ? dimmedColor : normalColor;

        countText.text = count > 1 ? count.ToString() : string.Empty;

        onClick = click;
        button.interactable = !dimmed && click != null;
    }
}
