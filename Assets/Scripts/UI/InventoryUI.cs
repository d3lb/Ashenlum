using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour {
    public static bool IsOpen { get; private set; }

    [Header("Hotkeys")]
    [SerializeField] private KeyCode toggleKey      = KeyCode.Tab;
    [SerializeField] private bool    closeWithEscape = true;

    [Header("References")]
    [SerializeField] private GameObject inventoryPanel;

    [Header("Ability Icons")]
    [SerializeField] private AbilityIconBinding[] abilityIcons;

    [SerializeField] private Color lockedColor = new Color(1f, 1f, 1f, 0.15f);
    [SerializeField] private Color unlockedColor = Color.white;

    [Header("Loadout")]
    [SerializeField] private AbilitySocketUI abilitySocket;
    [SerializeField] private TalismanSocketUI[] talismanSockets;

    // Shown when the loadout is read-only.
    [SerializeField] private GameObject loadoutLockedHint;

    [Header("Owned list")]
    [SerializeField] private Transform abilityListParent;
    [SerializeField] private Transform talismanListParent;
    [SerializeField] private Transform bundleListParent;
    [SerializeField] private InventoryEntryUI entryPrefab;

    [Header("Money")]
    [SerializeField] private LumenUI lumenUI;

    [System.Serializable]
    public class AbilityIconBinding {
        public AbilityType ability;
        public Image icon;
        public bool  hideWhenLocked = false;
    }

    private readonly List<GameObject> spawnedEntries = new();
    private PlayerHealth playerHealth;

    private void Start() {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        IsOpen = false;
    }

    // Torn down while open must not leave the game frozen.
    private void OnDisable() {
        if (!IsOpen) return;

        IsOpen = false;
        lumenUI?.Hide();
        TimeManager.Release(this);
    }

    private void Update() {
        // Ticks at timeScale 0, so the close hotkey keeps working.
        if (Input.GetKeyDown(toggleKey)) {
            if (IsOpen) Close();
            else        Open();
            return;
        }

        if (IsOpen && closeWithEscape && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    public void Open() {
        if (IsOpen) return;

        // One gate, so a new panel never has to be added here.
        if (UIState.Busy) return;

        IsOpen = true;
        if (inventoryPanel != null) inventoryPanel.SetActive(true);

        // Pinned, not left to fade on its timer.
        lumenUI?.Show();

        playerHealth = FindFirstObjectByType<PlayerHealth>();

        Refresh();
        TimeManager.Freeze(this);
    }

    public void Close() {
        if (!IsOpen) return;

        IsOpen = false;
        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        lumenUI?.Hide();
        TimeManager.Release(this);
    }

    public void Toggle() {
        if (IsOpen) Close();
        else        Open();
    }

    // Only the loadout is checkpoint-bound; consumables work anywhere.
    private static bool CanEditLoadout => CheckPoint.PlayerAtCheckpoint;

    public void Refresh() {
        if (loadoutLockedHint != null) loadoutLockedHint.SetActive(!CanEditLoadout);

        RefreshAbilities();
        RefreshSockets();
        RefreshOwnedList();
    }

    private void RefreshAbilities() {
        if (abilityIcons == null) return;

        // From the run profile, so the panel cannot disagree with the player.
        var run = GameManager.Instance != null ? GameManager.Instance.activeRun : null;

        foreach (var binding in abilityIcons) {
            if (binding == null || binding.icon == null) continue;

            bool unlocked = run != null && run.IsAbilityUnlocked(binding.ability);

            if (binding.hideWhenLocked) {
                binding.icon.gameObject.SetActive(unlocked);
            }
            else {
                binding.icon.gameObject.SetActive(true);
                binding.icon.color = unlocked ? unlockedColor : lockedColor;
            }
        }
    }

    private void RefreshSockets() {
        var run = GameManager.Instance.activeRun;
        bool editable = CanEditLoadout;

        if (abilitySocket != null)
            abilitySocket.Bind(run.equippedAbility,
                run.equippedAbility == null || !editable ? null : UnequipAbility);

        for (int i = 0; i < talismanSockets.Length; i++) {
            int slot = i;
            Talisman equipped = i < run.equippedTalismans.Length ? run.equippedTalismans[i] : null;

            talismanSockets[i].Bind(equipped,
                equipped == null || !editable ? null : () => Unequip(slot));
        }
    }

    private void RefreshOwnedList() {
        var run = GameManager.Instance.activeRun;
        bool editable = CanEditLoadout;

        ClearSpawned();

        foreach (ActiveAbility a in run.ownedAbilities) {
            ActiveAbility captured = a;
            bool equipped = run.equippedAbility == a;
            Spawn(abilityListParent, a.icon, 1, equipped || !editable,
                  equipped || !editable ? null : () => EquipAbility(captured));
        }
        if (run.ownedAbilities.Count == 0) SpawnEmpty(abilityListParent);

        foreach (Talisman t in run.ownedTalismans) {
            Talisman captured = t;
            bool equipped = run.IsEquipped(t);
            Spawn(talismanListParent, t.icon, 1, equipped || !editable,
                  equipped || !editable ? null : () => Equip(captured));
        }
        if (run.ownedTalismans.Count == 0) SpawnEmpty(talismanListParent);

        foreach (var pair in run.bundles) {
            LumenBundle captured = pair.Key;
            Spawn(bundleListParent, captured.icon, pair.Value, false, () => UseBundle(captured));
        }
        if (run.bundles.Count == 0) SpawnEmpty(bundleListParent);

        // Fitters recalculate next pass, so same-frame cells would overlap.
        RebuildLayout(talismanListParent);
    }

    // From the topmost layout group, so nested groups resolve.
    private static void RebuildLayout(Transform from) {
        RectTransform top = null;

        for (Transform p = from; p != null; p = p.parent)
            if (p is RectTransform rect && p.GetComponent<LayoutGroup>() != null)
                top = rect;

        if (top == null) return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(top);
    }

    // Only cells this script spawned.
    private void ClearSpawned() {
        foreach (GameObject go in spawnedEntries)
            if (go != null) Destroy(go);

        spawnedEntries.Clear();
    }

    private void Spawn(Transform parent, Sprite icon, int count, bool dimmed, System.Action click) {
        InventoryEntryUI entry = Instantiate(entryPrefab, parent, false);
        entry.Bind(icon, count, dimmed, click);
        spawnedEntries.Add(entry.gameObject);
    }

    private void SpawnEmpty(Transform parent) => Spawn(parent, null, 0, true, null);

    private void Equip(Talisman talisman) {
        if (!CanEditLoadout) return;

        // Both full: free one rather than silently swapping.
        if (!GameManager.Instance.activeRun.Equip(talisman)) return;

        GameManager.Instance.MarkDirty();
        RefreshPlayerMaxHealth();
        Refresh();
    }

    private void Unequip(int slot) {
        if (!CanEditLoadout) return;

        var run = GameManager.Instance.activeRun;
        if (slot < 0 || slot >= run.equippedTalismans.Length) return;

        run.Unequip(run.equippedTalismans[slot]);

        GameManager.Instance.MarkDirty();
        RefreshPlayerMaxHealth();
        Refresh();
    }

    private void EquipAbility(ActiveAbility ability) {
        if (!CanEditLoadout) return;
        if (!GameManager.Instance.activeRun.EquipAbility(ability)) return;

        GameManager.Instance.MarkDirty();
        Refresh();
    }

    private void UnequipAbility() {
        if (!CanEditLoadout) return;

        GameManager.Instance.activeRun.UnequipAbility();
        GameManager.Instance.MarkDirty();
        Refresh();
    }

    // MarkDirty already fired, so a null here saves a stale maxHp.
    private void RefreshPlayerMaxHealth() {
        if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();
        playerHealth?.RefreshMaxHealth();
    }

    private void UseBundle(LumenBundle bundle) {
        if (GameManager.Instance.UseBundle(bundle))
            Refresh();
    }
}
