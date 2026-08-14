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
    // Safe from death loss. Deposited at a shop.
    public int bankedLumens = 0;
    public HashSet<string> items = new();

    // How many times each Upgrade has been bought, keyed by Upgrade.Id.
    public Dictionary<string, int> upgradeCounts = new();

    public int bonusMaxHp = 0;
    public float bonusHealPercent = 0f;
    public int bonusDashes = 0;

    public int TimesPurchased(string upgradeId) =>
        upgradeCounts.TryGetValue(upgradeId, out int n) ? n : 0;

    public void ApplyUpgrade(Upgrade upgrade)
    {
        upgradeCounts[upgrade.Id] = TimesPurchased(upgrade.Id) + 1;

        switch (upgrade.type)
        {
            case UpgradeType.MaxHealth: bonusMaxHp       += Mathf.RoundToInt(upgrade.amount); break;
            case UpgradeType.BurstHeal: bonusHealPercent += upgrade.amount;                   break;
            case UpgradeType.Dash:      bonusDashes      += Mathf.RoundToInt(upgrade.amount); break;
        }
    }

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