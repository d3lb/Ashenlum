using UnityEngine;
using UnityEngine.UI;

// Sets fillAmount on the meter only; its colour is left alone.
public class AbilityCooldownUI : MonoBehaviour {
    [Header("Ability")]
    [SerializeField] private GameObject abilityGroup;
    [SerializeField] private Image abilityIcon;

    // The dark copy over the icon. Image Type must be Filled, and not the icon itself.
    [SerializeField] private Image abilityMeter;

    // A cover shrinks as it becomes ready; untick for a meter that grows.
    [SerializeField] private bool meterCoversIcon = true;

    [SerializeField] private Color iconReady    = Color.white;
    [SerializeField] private Color iconNotReady = new Color(1f, 1f, 1f, 0.6f);

    // No group toggle: the vent is never unequipped.
    [Header("Vent")]
    [SerializeField] private Image ventIcon;
    [SerializeField] private Image ventMeter;
    [SerializeField] private bool ventMeterCoversIcon = true;

    [Header("Dash")]
    [SerializeField] private GameObject dashGroup;
    // One per possible charge; spares hide themselves.
    [SerializeField] private DashPipUI[] dashPips;

    private PlayerActiveAbility ability;
    private PlayerAbility vent;
    private PlayerMovement movement;

    private void Awake() {
        if (abilityIcon != null && abilityIcon == abilityMeter)
            Debug.LogError("[AbilityCooldownUI] Ability Icon and Ability Meter are the same " +
                           "Image. The meter must be the dark copy on top, or nothing animates.",
                           this);

        if (ventIcon != null && ventIcon == ventMeter)
            Debug.LogError("[AbilityCooldownUI] Vent Icon and Vent Meter are the same Image. " +
                           "The meter must be the dark copy on top, or nothing animates.", this);
    }

    private void Update() {
        // The player is respawned, so these go stale.
        if (ability == null)  ability  = FindFirstObjectByType<PlayerActiveAbility>();
        if (vent == null)     vent     = FindFirstObjectByType<PlayerAbility>();
        if (movement == null) movement = FindFirstObjectByType<PlayerMovement>();

        UpdateAbility();
        UpdateVent();
        UpdateDash();
    }

    private void UpdateVent() {
        if (vent == null) return;

        // 0 while happening, 1 when done - same for charging and cooling down.
        bool charging = vent.IsCharging;
        float progress = charging ? vent.ChargePercent : vent.CooldownPercent;
        bool ready = !charging && progress >= 1f;

        if (ventIcon != null && ventIcon != ventMeter)
            ventIcon.color = ready ? iconReady : iconNotReady;

        if (ventMeter == null || ventMeter == ventIcon) return;

        ventMeter.fillAmount = ventMeterCoversIcon ? 1f - progress : progress;
    }

    private void UpdateAbility() {
        ActiveAbility equipped = ability != null ? ability.Equipped : null;

        if (abilityGroup != null) abilityGroup.SetActive(equipped != null);
        if (equipped == null) return;

        if (abilityIcon != null) {
            abilityIcon.sprite = equipped.icon;
            abilityIcon.enabled = equipped.icon != null;
        }

        // 0 while happening, 1 when done - same for charging and cooling down.
        bool charging = ability.IsCharging;
        float progress = charging ? ability.ChargePercent : ability.CooldownPercent;
        bool ready = !charging && progress >= 1f;

        if (abilityIcon != null && abilityIcon != abilityMeter)
            abilityIcon.color = ready ? iconReady : iconNotReady;

        if (abilityMeter == null || abilityMeter == abilityIcon) return;

        abilityMeter.fillAmount = meterCoversIcon ? 1f - progress : progress;
    }

    private void UpdateDash() {
        bool unlocked = GameManager.Instance != null &&
                        GameManager.Instance.activeRun.isDashUnlocked;

        bool show = unlocked && movement != null;

        if (dashGroup != null) dashGroup.SetActive(show);
        if (dashPips == null) return;

        // Driven even when locked, or pips linger in whatever state the editor left.
        int max  = show ? movement.DashChargesMax : 0;
        int held = show ? movement.DashCharges : 0;
        float refill = show ? movement.DashRefillPercent : 0f;

        for (int i = 0; i < dashPips.Length; i++) {
            DashPipUI pip = dashPips[i];
            if (pip == null) continue;

            // Talismans change the maximum.
            bool exists = i < max;
            pip.gameObject.SetActive(exists);
            if (!exists) continue;

            pip.Set(i < held, i == held ? refill : 0f);
        }
    }
}
