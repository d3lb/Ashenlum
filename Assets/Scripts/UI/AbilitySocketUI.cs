using UnityEngine;
using UnityEngine.UI;

// The single equipped active ability. Click to take it off.
public class AbilitySocketUI : MonoBehaviour {
    [SerializeField] private Image icon;
    [SerializeField] private GameObject emptyGraphic;
    [SerializeField] private Button button;

    private System.Action onClick;

    private void Awake() {
        button.onClick.AddListener(() => onClick?.Invoke());
    }

    public void Bind(ActiveAbility ability, System.Action click) {
        bool filled = ability != null;

        icon.sprite = filled ? ability.icon : null;
        icon.enabled = filled && ability.icon != null;

        if (emptyGraphic != null) emptyGraphic.SetActive(!filled);

        onClick = click;
        button.interactable = filled;
    }
}
