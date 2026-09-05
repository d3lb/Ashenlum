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

    [Header("Prompt")]
    [SerializeField] private string verb = "Take";

    protected override void Awake() {
        base.Awake();

        if (string.IsNullOrEmpty(rewardId))
            Debug.LogError($"[Reward] '{name}' has no Reward Id, so it cannot be saved.", this);

        if (coreAbility == null && activeAbility == null && talisman == null && strength == null)
            Debug.LogError($"[Reward] '{name}' grants nothing.", this);
    }

    // Start, not Awake, so GameManager is up.
    private void Start() {
        if (GameManager.Instance != null && GameManager.Instance.HasSeenEvent(rewardId))
            gameObject.SetActive(false);
    }

    protected override string PromptVerb => verb;

    protected override void Interact() {
        GameRunProfile run = GameManager.Instance.activeRun;

        // GrantAbility raises the card; talismans and shards grant silently.
        if (coreAbility != null) GameManager.Instance.GrantAbility(coreAbility.ability);
        if (activeAbility != null) GameManager.Instance.GrantAbility(activeAbility);

        if (talisman != null) run.AddTalisman(talisman);

        // SoldOut is the cap check the shop already uses.
        if (strength != null && !strength.SoldOut(run)) strength.Purchase(run);

        GameManager.Instance.RegisterEvent(rewardId);
        GameManager.Instance.MarkDirty();

        gameObject.SetActive(false);
    }
}
