using UnityEngine;
using UnityEngine.UI;

// The single equipped active ability. Click to take it off.
public class AbilitySocketUI : MonoBehaviour {
    [SerializeField] private Image icon;
    [SerializeField] private GameObject emptyGraphic;
    [SerializeField] private Button button;

    private System.Action onClick;
    private NotificationHover hover;

    private void Awake() {
        button.onClick.AddListener(() => onClick?.Invoke());
        hover = GetComponentInChildren<NotificationHover>(true);
    }

    public void Bind(ActiveAbility ability, System.Action click) {
        bool filled = ability != null;

        icon.sprite = filled ? ability.icon : null;
        icon.enabled = filled && ability.icon != null;

        if (emptyGraphic != null) emptyGraphic.SetActive(!filled);

        if (hover != null) {
            if (filled) hover.Set(ability.icon, ability.abilityName, ability.description);
            else        hover.Clear();
        }

        onClick = click;
        button.interactable = filled;
    }
}
