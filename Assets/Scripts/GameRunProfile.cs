using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameRunProfile
{
    [Header("Current Progress")]
    public string currentArea = "Cave_Entrance";
    public string targetEntranceId = "";
    public bool isTransitioningScenes = false;
    public int currentHp = 100;

    [Header("Inventory & Upgrades")]
    public int lumens = 0;
    public int cinders = 0;
    public List<string> items = new List<string>();

    [Header("Ability Unlocks")]
    public bool isDashUnlocked = false;
    public bool isDoubleJumpUnlocked = false;
    public bool isWallJumpUnlocked = false;

    [Header("World State")]
    public List<string> defeatedBosses = new List<string>();
    public List<string> openedShortcuts = new List<string>();
}