using UnityEngine;
using UnityEngine.UI;

// One equipped slot on the left. Click to take the talisman off.
public class TalismanSocketUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private GameObject emptyGraphic;
    [SerializeField] private Button button;

    private System.Action onClick;

    private void Awake()
    {
        button.onClick.AddListener(() => onClick?.Invoke());
    }

    public void Bind(Talisman talisman, System.Action click)
    {
        bool filled = talisman != null;

        icon.sprite = filled ? talisman.icon : null;
        icon.enabled = filled && talisman.icon != null;

        if (emptyGraphic != null) emptyGraphic.SetActive(!filled);

        onClick = click;
        button.interactable = filled;
    }
}
