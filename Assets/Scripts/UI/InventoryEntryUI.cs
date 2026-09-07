using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// InventoryUI hands it a sprite, its text and a click action; it knows nothing else.
public class InventoryEntryUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Button button;

    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color dimmedColor = new Color(1f, 1f, 1f, 0.3f);

    private System.Action onClick;
    private string title;
    private string body;

    private void Awake() {
        button.onClick.AddListener(() => onClick?.Invoke());
    }

    public void Bind(Sprite sprite, string name, string description,
                     int count, bool dimmed, System.Action click) {
        title = name;
        body = description;

        icon.sprite = sprite;
        icon.enabled = sprite != null;
        icon.color = dimmed ? dimmedColor : normalColor;

        countText.text = count > 1 ? count.ToString() : string.Empty;

        onClick = click;

        // Dimmed means equipped or locked, not empty - those still describe themselves.
        button.interactable = !dimmed && click != null;
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (string.IsNullOrEmpty(title)) return;

        Notification.Hold(icon.sprite, title, body);
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (string.IsNullOrEmpty(title)) return;

        Notification.Release();
    }
}
