using UnityEngine;
using UnityEngine.EventSystems;

// Goes on anything that describes itself on hover but is not an InventoryEntryUI - the core
// ability icons and the strength pips. Unknown entries still answer, with the text masked.
public class NotificationHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    private Sprite sprite;
    private string title;
    private string body;

    // Only the holder releases, so leaving one entry cannot close another's panel.
    private bool holding;

    public void Set(Sprite icon, string name, string description, bool known = true) {
        // Nothing of an unfound thing is shown, not even its shape.
        sprite = known ? icon : null;
        title = known ? name : Mask(name);
        body = known ? description : Mask(description);

        // Refreshed to nothing while the pointer is still on it.
        if (holding && string.IsNullOrEmpty(title)) Release();
    }

    public void Clear() => Set(null, null, null);

    // Whitespace survives, so the shape of the words stays readable as words.
    private static string Mask(string text) {
        if (string.IsNullOrEmpty(text)) return text;

        char[] chars = text.ToCharArray();

        for (int i = 0; i < chars.Length; i++)
            if (!char.IsWhiteSpace(chars[i])) chars[i] = '?';

        return new string(chars);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (string.IsNullOrEmpty(title)) return;

        holding = true;
        Notification.Hold(sprite, title, body);
    }

    public void OnPointerExit(PointerEventData eventData) => Release();

    private void OnDisable() => Release();

    private void Release() {
        if (!holding) return;

        holding = false;
        Notification.Release();
    }
}
