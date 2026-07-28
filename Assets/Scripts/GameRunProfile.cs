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
    public List<string> openedCheckpoints = new List<string>();
    public bool hasCheckpoint;
    public string checkpointScene;
    public string checkpointEntranceId;

    [Header("Inventory & Upgrades")]
    public int lumens = 0;
    public List<string> items = new List<string>();

    [Header("Ability Unlocks")]
    public bool isDashUnlocked = false;
    public bool isDoubleJumpUnlocked = false;
    public bool isWallJumpUnlocked = false;

    [Header("World State")]
    public List<string> defeatedBosses = new List<string>();
    public List<string> brokenWalls = new List<string>();
}