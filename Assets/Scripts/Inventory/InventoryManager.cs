using System.Collections.Generic;
using UnityEngine;

// Single entry point for player possessions: money, unlocked abilities and a
// fixed grid of item slots.
//
// IMPORTANT — money is deliberately NOT stored here. This project already tracks
// currency as "lumens" on GameManager.activeRun, and LumenPickup / LumenUI /
// CheatMenu all read and write it. A second counter in this class would silently
// desync from those, so AddMoney/SpendMoney just forward to GameManager and
// CurrentMoney reads straight through. One source of truth.
//
// Likewise, PlayerMovement gates dash/double-jump/wall-jump on the bools in
// GameRunProfile, so UnlockAbility writes those too — unlocking through this
// manager really does enable the move.
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Slots")]
    [SerializeField] private int slotCount = 20;

    private Item[] slots;

    // Abilities that have no legacy bool in GameRunProfile live here.
    private readonly HashSet<AbilityType> extraAbilities = new();

    public int SlotCount => slots != null ? slots.Length : slotCount;

    public int CurrentMoney => GameManager.Instance != null ? GameManager.Instance.CurrentLumens : 0;

    public System.Action              OnInventoryChanged;
    public System.Action<AbilityType> OnAbilityUnlocked;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        slots = new Item[Mathf.Max(1, slotCount)];
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Money ─────────────────────────────────────────────────────────────────
    public void AddMoney(int amount)
    {
        if (amount <= 0 || GameManager.Instance == null) return;
        GameManager.Instance.AddLumens(amount);   // fires OnLumensChanged for the HUD
    }

    // Returns false (and spends nothing) if the player can't afford it.
    public bool SpendMoney(int amount)
    {
        if (amount <= 0 || GameManager.Instance == null) return false;
        if (CurrentMoney < amount) return false;

        GameManager.Instance.TakeLumens(amount);
        return true;
    }

    // ── Abilities ─────────────────────────────────────────────────────────────
    public void UnlockAbility(AbilityType ability)
    {
        if (IsAbilityUnlocked(ability)) return;

        extraAbilities.Add(ability);
        WriteLegacyFlag(ability, true);

        OnAbilityUnlocked?.Invoke(ability);
        OnInventoryChanged?.Invoke();
    }

    public bool IsAbilityUnlocked(AbilityType ability)
    {
        // Legacy bools win for the three movement abilities so the CheatMenu
        // toggles stay authoritative.
        var run = GameManager.Instance != null ? GameManager.Instance.activeRun : null;
        if (run != null)
        {
            switch (ability)
            {
                case AbilityType.Dash:       return run.isDashUnlocked;
                case AbilityType.DoubleJump: return run.isDoubleJumpUnlocked;
                case AbilityType.WallJump:   return run.isWallJumpUnlocked;
            }
        }
        return extraAbilities.Contains(ability);
    }

    public IEnumerable<AbilityType> UnlockedAbilities
    {
        get
        {
            foreach (AbilityType a in System.Enum.GetValues(typeof(AbilityType)))
                if (IsAbilityUnlocked(a)) yield return a;
        }
    }

    private static void WriteLegacyFlag(AbilityType ability, bool value)
    {
        if (GameManager.Instance == null) return;
        var run = GameManager.Instance.activeRun;
        if (run == null) return;

        switch (ability)
        {
            case AbilityType.Dash:       run.isDashUnlocked       = value; break;
            case AbilityType.DoubleJump: run.isDoubleJumpUnlocked = value; break;
            case AbilityType.WallJump:   run.isWallJumpUnlocked   = value; break;
        }
    }

    // ── Items ─────────────────────────────────────────────────────────────────
    // Drops the item into the first empty slot. False = inventory full.
    public bool AddItem(Item item)
    {
        if (item == null || slots == null) return false;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null) continue;

            slots[i] = item;
            SyncItemIdsToProfile();
            OnInventoryChanged?.Invoke();
            return true;
        }

        Debug.LogWarning($"[InventoryManager] Inventory full — '{item.DisplayName}' was not picked up.", this);
        return false;
    }

    public bool RemoveItem(Item item)
    {
        if (item == null || slots == null) return false;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != item) continue;

            slots[i] = null;
            SyncItemIdsToProfile();
            OnInventoryChanged?.Invoke();
            return true;
        }
        return false;
    }

    public bool RemoveAt(int index)
    {
        if (slots == null || index < 0 || index >= slots.Length || slots[index] == null) return false;

        slots[index] = null;
        SyncItemIdsToProfile();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public Item GetItem(int index)
    {
        if (slots == null || index < 0 || index >= slots.Length) return null;
        return slots[index];
    }

    public bool HasItem(Item item)
    {
        if (item == null || slots == null) return false;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == item) return true;
        return false;
    }

    public bool IsFull()
    {
        if (slots == null) return true;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] == null) return false;
        return true;
    }

    public void ResetInventory()
    {
        slots = new Item[Mathf.Max(1, slotCount)];
        extraAbilities.Clear();
        SyncItemIdsToProfile();
        OnInventoryChanged?.Invoke();
    }

    // Mirrors held item ids into the run profile so they travel with the save
    // data that already exists on GameRunProfile.items.
    private void SyncItemIdsToProfile()
    {
        if (GameManager.Instance == null) return;
        var run = GameManager.Instance.activeRun;
        if (run == null || run.items == null) return;

        run.items.Clear();
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] != null) run.items.Add(slots[i].Id);
    }
}
