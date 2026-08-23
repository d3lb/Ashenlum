using UnityEngine;
using UnityEngine.UI;

// HUD readout for the equipped ability and the dash charges.
// This script only ever sets fillAmount on the meter. Its colour stays yours.
public class AbilityCooldownUI : MonoBehaviour
{
    [Header("Ability")]
    // Hidden as a whole when no ability is equipped.
    [SerializeField] private GameObject abilityGroup;
    [SerializeField] private Image abilityIcon;

    // The dark copy sitting on top of the icon. Image Type must be Filled or nothing
    // will move, and it must not be the same Image as the icon above.
    [SerializeField] private Image abilityMeter;

    // A cover shrinks away as the ability becomes ready. Untick if your meter is the
    // bright part that grows instead.
    [SerializeField] private bool meterCoversIcon = true;

    [SerializeField] private Color iconReady    = Color.white;
    [SerializeField] private Color iconNotReady = new Color(1f, 1f, 1f, 0.6f);

    [Header("Dash")]
    [SerializeField] private GameObject dashGroup;
    // One per possible charge. Pips past the current maximum hide themselves.
    [SerializeField] private DashPipUI[] dashPips;

    private PlayerActiveAbility ability;
    private PlayerMovement movement;

    private void Awake()
    {
        if (abilityIcon != null && abilityIcon == abilityMeter)
            Debug.LogError("[AbilityCooldownUI] Ability Icon and Ability Meter are the same " +
                           "Image. The meter must be the dark copy on top, or nothing animates.",
                           this);
    }

    private void Update()
    {
        // The player is respawned rather than kept, so the references go stale.
        if (ability == null)  ability  = FindFirstObjectByType<PlayerActiveAbility>();
        if (movement == null) movement = FindFirstObjectByType<PlayerMovement>();

        UpdateAbility();
        UpdateDash();
    }

    private void UpdateAbility()
    {
        ActiveAbility equipped = ability != null ? ability.Equipped : null;

        if (abilityGroup != null) abilityGroup.SetActive(equipped != null);
        if (equipped == null) return;

        if (abilityIcon != null)
        {
            abilityIcon.sprite = equipped.icon;
            abilityIcon.enabled = equipped.icon != null;
        }

        // 0 while it is happening, 1 when it is done. Charging and cooling down read
        // the same way, so the meter behaves consistently for both.
        bool charging = ability.IsCharging;
        float progress = charging ? ability.ChargePercent : ability.CooldownPercent;
        bool ready = !charging && progress >= 1f;

        if (abilityIcon != null && abilityIcon != abilityMeter)
            abilityIcon.color = ready ? iconReady : iconNotReady;

        if (abilityMeter == null || abilityMeter == abilityIcon) return;

        abilityMeter.fillAmount = meterCoversIcon ? 1f - progress : progress;
    }

    private void UpdateDash()
    {
        bool unlocked = GameManager.Instance != null &&
                        GameManager.Instance.activeRun.isDashUnlocked;

        bool show = unlocked && movement != null;

        if (dashGroup != null) dashGroup.SetActive(show);
        if (dashPips == null) return;

        // Driven even when locked, so pips can never linger in whatever state the
        // editor left them just because the group was never assigned.
        int max  = show ? movement.DashChargesMax : 0;
        int held = show ? movement.DashCharges : 0;
        float refill = show ? movement.DashRefillPercent : 0f;

        for (int i = 0; i < dashPips.Length; i++)
        {
            DashPipUI pip = dashPips[i];
            if (pip == null) continue;

            // Talismans change the maximum, so spare pips have to come and go.
            bool exists = i < max;
            pip.gameObject.SetActive(exists);
            if (!exists) continue;

            // Only the next charge in line refills; the rest wait their turn.
            pip.Set(i < held, i == held ? refill : 0f);
        }
    }
}
