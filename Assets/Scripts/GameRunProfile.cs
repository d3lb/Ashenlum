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
    public int currentHp = 100;

    [Header("Checkpoint Status")]
    public HashSet<string> openedCheckpoints = new();
    public bool hasCheckpoint;
    public string checkpointScene;
    public string checkpointEntranceId;

    [Header("Inventory & Upgrades")]
    public int lumens = 0;
    public HashSet<string> items = new();

    [Header("Ability Unlocks")]
    public bool isDashUnlocked = false;
    public bool isDoubleJumpUnlocked = false;
    public bool isWallJumpUnlocked = false;
    public bool isWingBurstUnlocked = false;

    // The profile is the single source of truth for unlocks - PlayerMovement and the
    // CheatMenu both read these bools directly. These two helpers just let UI ask the
    // question with an AbilityType instead of picking the right field by hand.
    public bool IsAbilityUnlocked(AbilityType ability)
    {
        switch (ability)
        {
            case AbilityType.Dash:       return isDashUnlocked;
            case AbilityType.DoubleJump: return isDoubleJumpUnlocked;
            case AbilityType.WallJump:   return isWallJumpUnlocked;
            case AbilityType.WingBurst:  return isWingBurstUnlocked;
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
            case AbilityType.WingBurst:  isWingBurstUnlocked  = value; break;
        }
    }

    [Header("World State")]
    public HashSet<string> defeatedBosses = new();
    public HashSet<string> brokenWalls = new();
    public HashSet<string> temporaryRemoved = new();
    public HashSet<string> permanentRemoved = new();
}