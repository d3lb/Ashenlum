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

    // Where loading resumes. Separate from the checkpoint: dying always uses the checkpoint.
    public enum ResumeType { None, Checkpoint, Entrance }

    public ResumeType resumeType = ResumeType.None;
    public string     resumeScene;
    public string     resumeId;

    [Header("Checkpoint Status")]
    public HashSet<string> openedCheckpoints = new();
    public bool hasCheckpoint;
    public string checkpointScene;
    public string checkpointEntranceId;

    [Header("Inventory & Upgrades")]
    public int lumens = 0;

    public int     droppedLumens;
    public string  dropScene;
    public Vector2 dropPosition;

    public bool HasShade => droppedLumens > 0;

    public Dictionary<LumenBundle, int> bundles = new();

    public int BundleCount(LumenBundle bundle) =>
        bundle != null && bundles.TryGetValue(bundle, out int n) ? n : 0;

    public void AddBundle(LumenBundle bundle)
    {
        if (bundle == null) return;
        bundles[bundle] = BundleCount(bundle) + 1;
    }

    public bool ConsumeBundle(LumenBundle bundle)
    {
        int held = BundleCount(bundle);
        if (held <= 0) return false;

        if (held == 1) bundles.Remove(bundle);
        else           bundles[bundle] = held - 1;

        return true;
    }

    public const int TalismanSlots = 2;

    public int strengthLevel = 0;

    public List<Talisman> ownedTalismans = new();
    public Talisman[] equippedTalismans = new Talisman[TalismanSlots];

    public bool Owns(Talisman t) => t != null && ownedTalismans.Contains(t);

    public void AddTalisman(Talisman t)
    {
        if (t != null && !ownedTalismans.Contains(t)) ownedTalismans.Add(t);
    }

    public bool IsEquipped(Talisman t) =>
        t != null && System.Array.IndexOf(equippedTalismans, t) >= 0;

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

    // SAVING

    public RunSave ToSave()
    {
        RunSave save = new RunSave
        {
            currentArea          = currentArea,
            targetEntranceId     = targetEntranceId,
            resumeType           = (int)resumeType,
            resumeScene          = resumeScene,
            resumeId             = resumeId,
            maxHp                = maxHp,
            currentHp            = currentHp,
            hasCheckpoint        = hasCheckpoint,
            checkpointScene      = checkpointScene,
            checkpointEntranceId = checkpointEntranceId,
            lumens               = lumens,
            strengthLevel        = strengthLevel,
            equippedAbility      = equippedAbility != null ? equippedAbility.Id : "",
            isDashUnlocked       = isDashUnlocked,
            isDoubleJumpUnlocked = isDoubleJumpUnlocked,
            isWallJumpUnlocked   = isWallJumpUnlocked,
            droppedLumens        = droppedLumens,
            dropScene            = dropScene,
            dropPosition         = dropPosition
        };

        save.openedCheckpoints.AddRange(openedCheckpoints);
        save.seenEvents.AddRange(seenEvents);
        save.defeatedBosses.AddRange(defeatedBosses);
        save.brokenWalls.AddRange(brokenWalls);
        save.permanentRemoved.AddRange(permanentRemoved);

        foreach (Talisman t in ownedTalismans)
            if (t != null) save.ownedTalismans.Add(t.Id);

        foreach (Talisman t in equippedTalismans)
            save.equippedTalismans.Add(t != null ? t.Id : "");

        foreach (var pair in bundles)
        {
            if (pair.Key == null) continue;
            save.bundleIds.Add(pair.Key.Id);
            save.bundleCounts.Add(pair.Value);
        }

        foreach (ActiveAbility a in ownedAbilities)
            if (a != null) save.ownedAbilities.Add(a.Id);

        return save;
    }

    public void ApplySave(RunSave save, GameAssetDatabase db)
    {
        if (save == null) return;

        currentArea          = save.currentArea;
        targetEntranceId     = save.targetEntranceId;
        resumeType           = (ResumeType)save.resumeType;
        resumeScene          = save.resumeScene;
        resumeId             = save.resumeId;
        maxHp                = save.maxHp;
        currentHp            = save.currentHp;
        hasCheckpoint        = save.hasCheckpoint;
        checkpointScene      = save.checkpointScene;
        checkpointEntranceId = save.checkpointEntranceId;
        lumens               = save.lumens;
        strengthLevel        = save.strengthLevel;
        isDashUnlocked       = save.isDashUnlocked;
        isDoubleJumpUnlocked = save.isDoubleJumpUnlocked;
        isWallJumpUnlocked   = save.isWallJumpUnlocked;
        droppedLumens        = save.droppedLumens;
        dropScene            = save.dropScene;
        dropPosition         = save.dropPosition;

        openedCheckpoints = new HashSet<string>(save.openedCheckpoints);
        seenEvents        = new HashSet<string>(save.seenEvents);
        defeatedBosses    = new HashSet<string>(save.defeatedBosses);
        brokenWalls       = new HashSet<string>(save.brokenWalls);
        permanentRemoved  = new HashSet<string>(save.permanentRemoved);
        temporaryRemoved  = new HashSet<string>();

        ownedTalismans = new List<Talisman>();
        equippedTalismans = new Talisman[TalismanSlots];
        bundles = new Dictionary<LumenBundle, int>();
        ownedAbilities = new List<ActiveAbility>();
        equippedAbility = null;

        if (db == null) return;

        foreach (string id in save.ownedTalismans)
        {
            Talisman t = db.FindGood<Talisman>(id);
            if (t != null) ownedTalismans.Add(t);
        }

        for (int i = 0; i < equippedTalismans.Length && i < save.equippedTalismans.Count; i++)
            equippedTalismans[i] = db.FindGood<Talisman>(save.equippedTalismans[i]);

        for (int i = 0; i < save.bundleIds.Count && i < save.bundleCounts.Count; i++)
        {
            LumenBundle bundle = db.FindGood<LumenBundle>(save.bundleIds[i]);
            if (bundle != null && save.bundleCounts[i] > 0) bundles[bundle] = save.bundleCounts[i];
        }

        foreach (string id in save.ownedAbilities)
        {
            ActiveAbility a = db.FindAbility(id);
            if (a != null) ownedAbilities.Add(a);
        }

        equippedAbility = db.FindAbility(save.equippedAbility);
    }

    [Header("World State")]
    public HashSet<string> seenEvents = new();
    public HashSet<string> defeatedBosses = new();
    public HashSet<string> brokenWalls = new();
    public HashSet<string> temporaryRemoved = new();
    public HashSet<string> permanentRemoved = new();
}