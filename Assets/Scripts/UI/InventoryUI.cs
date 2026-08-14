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
    [SerializeField] private GameObject      inventoryPanel;
    [SerializeField] private Transform       slotGridParent;
    [SerializeField] private InventorySlotUI slotPrefab;

    [Header("Ability Icons")]
    [SerializeField] private AbilityIconBinding[] abilityIcons;


    [SerializeField] private Color lockedColor = new Color(1f, 1f, 1f, 0.15f);
    [SerializeField] private Color unlockedColor = Color.white;

    [Header("Money")]
    [SerializeField] private LumenUI lumenUI;

    [Header("Slots")]
    [SerializeField] private int slotCount = 20;

    [System.Serializable]
    public class AbilityIconBinding
    {
        public AbilityType ability;
        public Image icon;
        public bool  hideWhenLocked = false;
    }

    private readonly List<InventorySlotUI> spawnedSlots = new();

    private void Start()
    {
        BuildGrid();

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

        for (int i = 0; i < Mathf.Max(1, slotCount); i++)
        {
            var slot = Instantiate(slotPrefab, slotGridParent);
            slot.name = $"Slot_{i:D2}";
            slot.Clear();
            spawnedSlots.Add(slot);
        }
    }

    public void Refresh()
    {
        RefreshAbilities();
        RefreshSlots();
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

    // There is no item storage yet. When you build one, this is the only place that
    // needs to change - hand each slot its Item instead of null.
    private void RefreshSlots()
    {
        for (int i = 0; i < spawnedSlots.Count; i++)
            spawnedSlots[i].SetItem(null);
    }
}
