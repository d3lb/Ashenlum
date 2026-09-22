using UnityEngine;

// Grants whatever is assigned, once, and never comes back. No kind field: the filled slot
// already says what this is.
public class Reward : Interactable {
    [SerializeField] private string rewardId;

    [Header("Grants (fill one)")]
    [SerializeField] private CoreAbilityInfo coreAbility;
    [SerializeField] private ActiveAbility activeAbility;
    [SerializeField] private Talisman talisman;

    // Goes through the shop asset, so the level cap lives in one place.
    [SerializeField] private StrengthUpgrade strength;

    // Shows whatever is assigned above. Reading the same icon the inventory reads means the
    // pedestal cannot end up advertising something different from what it hands over.
    [Header("Showcase")]
    [SerializeField] private SpriteRenderer showcase;

    [Header("Prompt")]
    [SerializeField] private string verb = "Take";

    protected override void Awake() {
        base.Awake();

        if (string.IsNullOrEmpty(rewardId))
            Debug.LogError($"[Reward] '{name}' has no Reward Id, so it cannot be saved.", this);

        if (coreAbility == null && activeAbility == null && talisman == null && strength == null)
            Debug.LogError($"[Reward] '{name}' grants nothing.", this);
    }

    // Same order Interact grants in, so the picture matches whichever slot wins.
    private Sprite PrizeIcon() {
        if (coreAbility != null) return coreAbility.icon;
        if (activeAbility != null) return activeAbility.icon;
        if (talisman != null) return talisman.icon;
        if (strength != null) return strength.icon;

        return null;
    }

    private void ShowPrize() {
        if (showcase == null) return;

        Sprite icon = PrizeIcon();

        showcase.sprite = icon;

        // Both switches: a renderer left disabled on the prefab would hide the prize even
        // with the object active and the sprite set.
        showcase.enabled = true;
        showcase.gameObject.SetActive(icon != null);
    }

#if UNITY_EDITOR
    // Sprite only. ShowPrize also toggles the object, and doing that from OnValidate wrote
    // an inactive Showcase straight into the prefab asset - every instance inherited it.
    //
    // Deferred, because assigning a sprite sends bounds and tiling messages that Unity
    // forbids during OnValidate. The null check is for being deleted before it runs.
    private void OnValidate() {
        UnityEditor.EditorApplication.delayCall += () => {
            if (this == null || showcase == null) return;

            showcase.sprite = PrizeIcon();
        };
    }
#endif

    // Start, not Awake: GameManager is up by then, and an exception in the base Awake cannot
    // stop the showcase from being set.
    private void Start() {
        ShowPrize();

        if (GameManager.Instance != null && GameManager.Instance.HasSeenEvent(rewardId))
            MarkTaken();
    }

    private bool taken;

    protected override bool CanInteract => !taken;
    protected override string PromptVerb => verb;

    // The pedestal stays: only the prize sitting on it goes. SetActive, not renderer.enabled,
    // so the light and anything else parented under the picture goes with it.
    private void MarkTaken() {
        taken = true;

        if (showcase != null) showcase.gameObject.SetActive(false);
    }

    protected override void Interact() {
        GameRunProfile run = GameManager.Instance.activeRun;

        // GrantAbility raises the card; talismans and shards grant silently.
        if (coreAbility != null) GameManager.Instance.GrantAbility(coreAbility.ability);
        if (activeAbility != null) GameManager.Instance.GrantAbility(activeAbility);

        // Both raise their own Notification from inside the run profile.
        if (talisman != null) run.AddTalisman(talisman);

        // SoldOut is the cap check the shop already uses.
        if (strength != null && !strength.SoldOut(run)) strength.Purchase(run);

        GameManager.Instance.RegisterEvent(rewardId);
        GameManager.Instance.MarkDirty();

        MarkTaken();
    }
}
