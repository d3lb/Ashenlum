using System.Collections.Generic;
using UnityEngine;

// Plain data only. JsonUtility cannot write a HashSet, a Dictionary or an asset
// reference, so everything the run profile holds gets flattened into lists and ids here.

[System.Serializable]
public class ProfileEntry
{
    public int    profileId;
    public bool   slotUsed;
    public float  playTime;
    public int    deaths;
    public string saveFile;
}

[System.Serializable]
public class ProfileIndex
{
    public int lastUsedProfile = -1;
    public List<ProfileEntry> profiles = new();

    public ProfileEntry Get(int profileId)
    {
        foreach (ProfileEntry entry in profiles)
            if (entry.profileId == profileId) return entry;

        return null;
    }
}

[System.Serializable]
public class RunSave
{
    [Header("Position")]
    public string currentArea;
    public string targetEntranceId;

    [Header("Health")]
    public int maxHp;
    public int currentHp;

    [Header("Checkpoints")]
    public bool         hasCheckpoint;
    public string       checkpointScene;
    public string       checkpointEntranceId;
    public List<string> openedCheckpoints = new();

    [Header("Money")]
    public int lumens;

    // Two parallel lists rather than a dictionary, which JsonUtility will not write.
    public List<string> bundleIds    = new();
    public List<int>    bundleCounts = new();

    [Header("Upgrades")]
    public int          strengthLevel;
    public List<string> ownedTalismans    = new();
    public List<string> equippedTalismans = new();

    public List<string> ownedAbilities = new();
    public string       equippedAbility;

    [Header("Core Abilities")]
    public bool isDashUnlocked;
    public bool isDoubleJumpUnlocked;
    public bool isWallJumpUnlocked;

    [Header("World")]
    public List<string> seenEvents      = new();
    public List<string> defeatedBosses  = new();
    public List<string> brokenWalls     = new();
    public List<string> permanentRemoved = new();

    [Header("Shade")]
    public int     droppedLumens;
    public string  dropScene;
    public Vector2 dropPosition;
}
