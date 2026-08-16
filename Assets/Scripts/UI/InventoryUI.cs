using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
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
    [SerializeField] private TalismanSocketUI[] talismanSockets;

    [Header("Owned list")]
    // The grid inside each section. Sections themselves are laid out in the editor -
    // there are two, they never change, so there is nothing to build at runtime.
    [SerializeField] private Transform abilityListParent;
    [SerializeField] private Transform talismanListParent;
    [SerializeField] private Transform bundleListParent;
    [SerializeField] private InventoryEntryUI entryPrefab;

    [Header("Money")]
    [SerializeField] private LumenUI lumenUI;

    [System.Serializable]
    public class AbilityIconBinding
    {
        public AbilityType ability;
        public Image icon;
        public bool  hideWhenLocked = false;
    }

    private readonly List<GameObject> spawnedEntries = new();
    private PlayerHealth playerHealth;

    private void Start()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        IsOpen = false;
    }

    // Safety net: never leave the game frozen if this object goes away while open.
    private void OnDisable()
    {
        if (!IsOpen) return;

        IsOpen = false;
        lumenUI?.Hide();
        TimeManager.Release(this);
    }

    private void Update()
    {
        // Update() still ticks at timeScale 0, so the close hotkey keeps working.
        if (Input.GetKeyDown(toggleKey))
        {
            if (IsOpen) Close();
            else        Open();
            return;
        }

        if (IsOpen && closeWithEscape && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    public void Open()
    {
        if (IsOpen) return;

        // Don't stack on top of the pause menu or a conversation.
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return;
        if (DialogueManager.IsDialogueActive) return;
        if (ShopUI.IsOpen) return;

        IsOpen = true;
        if (inventoryPanel != null) inventoryPanel.SetActive(true);

        // Same trick the pause menu uses: pin the lumen counter open instead of
        // letting it fade out on its usual timer.
        lumenUI?.Show();

        playerHealth = FindFirstObjectByType<PlayerHealth>();

        Refresh();
        TimeManager.Freeze(this);
    }

    public void Close()
    {
        if (!IsOpen) return;

        IsOpen = false;
        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        lumenUI?.Hide();
        TimeManager.Release(this);
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else        Open();
    }

    public void Refresh()
    {
        RefreshAbilities();
        RefreshSockets();
        RefreshOwnedList();
    }

    private void RefreshAbilities()
    {
        if (abilityIcons == null) return;

        // Straight from the run profile - the same bools PlayerMovement and the
        // CheatMenu use, so the panel can never disagree with what the player can do.
        var run = GameManager.Instance != null ? GameManager.Instance.activeRun : null;

        foreach (var binding in abilityIcons)
        {
            if (binding == null || binding.icon == null) continue;

            bool unlocked = run != null && run.IsAbilityUnlocked(binding.ability);

            if (binding.hideWhenLocked)
            {
                binding.icon.gameObject.SetActive(unlocked);
            }
            else
            {
                binding.icon.gameObject.SetActive(true);
                binding.icon.color = unlocked ? unlockedColor : lockedColor;
            }
        }
    }

    private void RefreshSockets()
    {
        var run = GameManager.Instance.activeRun;

        for (int i = 0; i < talismanSockets.Length; i++)
        {
            int slot = i;
            Talisman equipped = i < run.equippedTalismans.Length ? run.equippedTalismans[i] : null;

            talismanSockets[i].Bind(equipped, equipped == null ? null : () => Unequip(slot));
        }
    }

    private void RefreshOwnedList()
    {
        var run = GameManager.Instance.activeRun;

        ClearSpawned();

        // Active abilities do not exist as a type yet - the section is here so the
        // category is visible, and it fills itself once there is something to own.
        SpawnEmpty(abilityListParent);

        foreach (Talisman t in run.ownedTalismans)
        {
            Talisman captured = t;
            bool equipped = run.IsEquipped(t);
            Spawn(talismanListParent, t.icon, 1, equipped, equipped ? null : () => Equip(captured));
        }
        if (run.ownedTalismans.Count == 0) SpawnEmpty(talismanListParent);

        var bundles = BundlesHeld(run);
        foreach (var pair in bundles)
        {
            LumenBundle captured = pair.Key;
            Spawn(bundleListParent, captured.icon, pair.Value, false, () => UseBundle(captured));
        }
        if (bundles.Count == 0) SpawnEmpty(bundleListParent);

        // Fitters only recalculate on the next layout pass, so cells spawned and shown in
        // the same frame draw on top of each other. Force it now instead.
        RebuildLayout(talismanListParent);
    }

    // Rebuilds from the highest layout group above this transform, so nested
    // grid -> section -> content all resolve in one go.
    private static void RebuildLayout(Transform from)
    {
        RectTransform top = null;

        for (Transform p = from; p != null; p = p.parent)
            if (p is RectTransform rect && p.GetComponent<LayoutGroup>() != null)
                top = rect;

        if (top == null) return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(top);
    }

    // Only removes cells this script created. Anything you placed by hand in the editor
    // is left alone - it is your layout, not ours to delete.
    private void ClearSpawned()
    {
        foreach (GameObject go in spawnedEntries)
            if (go != null) Destroy(go);

        spawnedEntries.Clear();
    }

    // Bundles are stored as id -> count, so the asset has to come from the shop stock
    // the player bought from. Held bundles are looked up through the shops in the scene.
    private Dictionary<LumenBundle, int> BundlesHeld(GameRunProfile run)
    {
        var held = new Dictionary<LumenBundle, int>();

        foreach (Shop shop in FindObjectsByType<Shop>(FindObjectsSortMode.None))
            foreach (ShopGood good in shop.Stock)
                if (good is LumenBundle bundle && !held.ContainsKey(bundle))
                {
                    int count = run.BundleCount(bundle.Id);
                    if (count > 0) held[bundle] = count;
                }

        return held;
    }


    private void Spawn(Transform parent, Sprite icon, int count, bool dimmed, System.Action click)
    {
        InventoryEntryUI entry = Instantiate(entryPrefab, parent);
        entry.Bind(icon, count, dimmed, click);
        spawnedEntries.Add(entry.gameObject);
    }

    // A blank cell: no icon, no count, not clickable. Just the socket art.
    private void SpawnEmpty(Transform parent) => Spawn(parent, null, 0, true, null);

    private void Equip(Talisman talisman)
    {
        // Both sockets full - free one first rather than silently swapping.
        if (!GameManager.Instance.activeRun.Equip(talisman)) return;

        playerHealth?.RefreshMaxHealth();
        Refresh();
    }

    private void Unequip(int slot)
    {
        var run = GameManager.Instance.activeRun;
        if (slot < 0 || slot >= run.equippedTalismans.Length) return;

        run.Unequip(run.equippedTalismans[slot]);

        playerHealth?.RefreshMaxHealth();
        Refresh();
    }

    private void UseBundle(LumenBundle bundle)
    {
        if (GameManager.Instance.activeRun.UseBundle(bundle))
            Refresh();
    }
}
