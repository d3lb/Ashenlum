using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Toggles the inventory screen, freezes the game while it's open, and mirrors
// InventoryManager's data into the visual grid.
//
// The slot cells are spawned from a prefab at Start (slotGridParent should carry
// a GridLayoutGroup, which handles all the positioning), then simply refreshed
// whenever the data changes.
public class InventoryUI : MonoBehaviour
{
    // Static so PlayerInput / PauseManager can gate on it with no reference.
    public static bool IsOpen { get; private set; }

    [Header("Hotkeys")]
    [SerializeField] private KeyCode toggleKey      = KeyCode.Tab;
    [SerializeField] private bool    closeWithEscape = true;

    [Header("References")]
    [SerializeField] private GameObject      inventoryPanel;
    [Tooltip("The object carrying the GridLayoutGroup. Slot prefabs are spawned as its children.")]
    [SerializeField] private Transform       slotGridParent;
    [SerializeField] private InventorySlotUI slotPrefab;
    [SerializeField] private TMP_Text        moneyText;

    [Header("Ability Icons")]
    [SerializeField] private AbilityIconBinding[] abilityIcons;

    [Header("Fallback")]
    [Tooltip("Used only if no InventoryManager is present (e.g. testing this scene alone).")]
    [SerializeField] private int fallbackSlotCount = 20;

    [System.Serializable]
    public class AbilityIconBinding
    {
        public AbilityType ability;
        [Tooltip("The icon Image for this ability.")]
        public Image icon;
        [Tooltip("Hide the icon entirely while locked instead of dimming it.")]
        public bool  hideWhenLocked = false;
        public Color lockedColor    = new Color(1f, 1f, 1f, 0.15f);
        public Color unlockedColor  = Color.white;
    }

    private readonly List<InventorySlotUI> spawnedSlots = new();

    private void Start()
    {
        BuildGrid();

        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        IsOpen = false;

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += Refresh;

        if (GameManager.Instance != null)
            GameManager.Instance.OnLumensChanged += OnMoneyChanged;
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= Refresh;

        if (GameManager.Instance != null)
            GameManager.Instance.OnLumensChanged -= OnMoneyChanged;
    }

    // Safety net: never leave the game frozen if this object goes away while open.
    private void OnDisable()
    {
        if (!IsOpen) return;
        IsOpen = false;
        Time.timeScale = 1f;
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

    // ── Open / Close ──────────────────────────────────────────────────────────
    public void Open()
    {
        if (IsOpen) return;

        // Don't stack on top of the pause menu or a conversation.
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return;
        if (DialogueManager.IsDialogueActive) return;

        IsOpen = true;
        if (inventoryPanel != null) inventoryPanel.SetActive(true);

        Refresh();
        Time.timeScale = 0f;
    }

    public void Close()
    {
        if (!IsOpen) return;

        IsOpen = false;
        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        Time.timeScale = 1f;
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else        Open();
    }

    // ── Building / refreshing ─────────────────────────────────────────────────
    private void BuildGrid()
    {
        if (slotGridParent == null || slotPrefab == null)
        {
            Debug.LogError("[InventoryUI] Slot Grid Parent and Slot Prefab must be assigned.", this);
            return;
        }

        // Clear anything the designer left in the grid so we never double up.
        for (int i = slotGridParent.childCount - 1; i >= 0; i--)
            Destroy(slotGridParent.GetChild(i).gameObject);
        spawnedSlots.Clear();

        int count = InventoryManager.Instance != null
            ? InventoryManager.Instance.SlotCount
            : fallbackSlotCount;

        for (int i = 0; i < count; i++)
        {
            var slot = Instantiate(slotPrefab, slotGridParent);
            slot.name = $"Slot_{i:D2}";
            slot.Clear();
            spawnedSlots.Add(slot);
        }
    }

    private void OnMoneyChanged(int _) => RefreshMoney();

    public void Refresh()
    {
        RefreshMoney();
        RefreshAbilities();
        RefreshSlots();
    }

    private void RefreshMoney()
    {
        if (moneyText == null) return;

        int money = InventoryManager.Instance != null
            ? InventoryManager.Instance.CurrentMoney
            : (GameManager.Instance != null ? GameManager.Instance.CurrentLumens : 0);

        moneyText.text = money.ToString();
    }

    private void RefreshAbilities()
    {
        if (abilityIcons == null) return;

        var inv = InventoryManager.Instance;

        foreach (var binding in abilityIcons)
        {
            if (binding == null || binding.icon == null) continue;

            bool unlocked = inv != null && inv.IsAbilityUnlocked(binding.ability);

            if (binding.hideWhenLocked)
            {
                binding.icon.gameObject.SetActive(unlocked);
            }
            else
            {
                binding.icon.gameObject.SetActive(true);
                binding.icon.color = unlocked ? binding.unlockedColor : binding.lockedColor;
            }
        }
    }

    private void RefreshSlots()
    {
        var inv = InventoryManager.Instance;

        for (int i = 0; i < spawnedSlots.Count; i++)
            spawnedSlots[i].SetItem(inv != null ? inv.GetItem(i) : null);
    }
}
