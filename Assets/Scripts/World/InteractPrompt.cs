using TMPro;
using UnityEngine;

// The floating "E - Talk" above an interactable. One per interactable, added as a child
// prefab. Owns nothing but its own visibility and text - Interactable drives it.
public class InteractPrompt : MonoBehaviour
{
    [SerializeField] private GameObject visual;
    [SerializeField] private TMP_Text label;
    [SerializeField] private string keyName = "E";

    private void Awake()
    {
        visual.SetActive(false);
    }

    public void Show(string verb)
    {
        label.text = $"{keyName} - {verb}";
        if (!visual.activeSelf) visual.SetActive(true);
    }

    public void Hide()
    {
        if (visual.activeSelf) visual.SetActive(false);
    }
}
