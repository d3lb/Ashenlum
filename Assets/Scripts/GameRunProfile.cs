using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameRunProfile
{
    [Header("Current Progress")]
    public string currentArea = "Start";
    public string targetEntranceId = "";
    public bool isTransitioningScenes = false;
    public int maxHp = 100;
    public int currentHp = -1;

    [Header("Checkpoint Status")]
    public HashSet<string> openedCheckpoints = new();
    public bool hasCheckpoint;
    public string checkpointScene;
    public string checkpointEntranceId;

    [Header("Inventory & Upgrades")]
    public int lumens = 0;

    // Bundles survive death. Keyed by the asset itself, so nothing has to hunt down
    // which shop sold it to turn an id back into something the inventory can draw.
    public Dictionary<LumenBundle, int> bundles = new();

    public int BundleCount(LumenBundle bundle) =>
        bundle != null && bundles.TryGetValue(bundle, out int n) ? n : 0;

    public void AddBundle(LumenBundle bundle)
    {
        if (bundle == null) return;
        bundles[bundle] = BundleCount(bundle) + 1;
    }

    // Spends the bundle but does NOT pay out - GameManager.UseBundle does that, so
    // every change to lumens goes through the one place that raises OnLumensChanged.
    public bool ConsumeBundle(LumenBundle bundle)
    {
        int held = BundleCount(bundle);
        if (held <= 0) return false;

        // Drop the key at zero so the inventory can just walk the dictionary.
        if (held == 1) bundles.Remove(bundle);
        else           bundles[bundle] = held - 1;

        return true;
    }

    public const int TalismanSlots = 2;

    public int strengthLevel = 0;

    // Bought talismans are kept forever - they cannot be sold or dropped.
    public List<Talisman> ownedTalismans = new();
    public Talisman[] equippedTalismans = new Talisman[TalismanSlots];

    public bool Owns(Talisman t) => t != null && ownedTalismans.Contains(t);

    public void AddTalisman(Talisman t)
    {
        if (t != null && !ownedTalismans.Contains(t)) ownedTalismans.Add(t);
    }

    public bool IsEquipped(Talisman t) =>
        t != null && System.Array.IndexOf(equippedTalismans, t) >= 0;

    // Into the first free slot. False if you own none of it, or both slots are taken.
    public bool Equip(Talisman t)
    {
        if (!Owns(t) || IsEquipped(t)) return false;

        for (int i = 0; i < equippedTalismans.Length; i++)
        {
            if (equippedTalismans[i] != null) continue;
            equippedTalismans[i] = t;
            return true;
        }
        return false;
    }

    public void Unequip(Talisman t)
    {
        for (int i = 0; i < equippedTalismans.Length; i++)
            if (equippedTalismans[i] == t) equippedTalismans[i] = null;
    }

    // Derived, never banked - that is what lets a talisman be taken off again.
    public int bonusMaxHp => Mathf.RoundToInt(Sum(TalismanType.MaxHealth));
    public float bonusHealPercent => Sum(TalismanType.BurstHeal);
    public int bonusDashes => Mathf.RoundToInt(Sum(TalismanType.Dash));

    private float Sum(TalismanType type)
    {
        float total = 0f;
        foreach (Talisman t in equippedTalismans)
            if (t != null && t.type == type) total += t.amount;
        return total;
    }

    // One socket, so equipping just replaces whatever was in it.
    public List<ActiveAbility> ownedAbilities = new();
    public ActiveAbility equippedAbility;

    public bool OwnsAbility(ActiveAbility a) => a != null && ownedAbilities.Contains(a);

    public void AddAbility(ActiveAbility a)
    {
        if (a != null && !ownedAbilities.Contains(a)) ownedAbilities.Add(a);
    }

    public bool EquipAbility(ActiveAbility a)
    {
        if (!OwnsAbility(a)) return false;

        equippedAbility = a;
        return true;
    }

    public void UnequipAbility() => equippedAbility = null;

    [Header("Ability Unlocks")]
    public bool isDashUnlocked = false;
    public bool isDoubleJumpUnlocked = false;
    public bool isWallJumpUnlocked = false;


    public bool IsAbilityUnlocked(AbilityType ability)
    {
        switch (ability)
        {
            case AbilityType.Dash:       return isDashUnlocked;
            case AbilityType.DoubleJump: return isDoubleJumpUnlocked;
            case AbilityType.WallJump:   return isWallJumpUnlocked;
            default:                     return false;
        }
    }

    public void SetAbilityUnlocked(AbilityType ability, bool value)
    {
        switch (ability)
        {
            case AbilityType.Dash:       isDashUnlocked       = value; break;
            case AbilityType.DoubleJump: isDoubleJumpUnlocked = value; break;
            case AbilityType.WallJump:   isWallJumpUnlocked   = value; break;
        }
    }

    [Header("World State")]
    public HashSet<string> seenEvents = new();
    public HashSet<string> defeatedBosses = new();
    public HashSet<string> brokenWalls = new();
    public HashSet<string> temporaryRemoved = new();
    public HashSet<string> permanentRemoved = new();
}