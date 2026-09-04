using System.IO;
using UnityEngine;

// Profile index plus one run file each, in Application.persistentDataPath.
public static class SaveSystem {
    public const int SlotCount = 3;

    private const string IndexFile = "save_slot.json";

    private static string PathTo(string file) => Path.Combine(Application.persistentDataPath, file);

    public static string RunFileName(int profileId) => $"save_run_{profileId:00}.json";

    // INDEX

    public static ProfileIndex LoadIndex() {
        string path = PathTo(IndexFile);
        if (!File.Exists(path)) return new ProfileIndex();

        try {
            return JsonUtility.FromJson<ProfileIndex>(File.ReadAllText(path)) ?? new ProfileIndex();
        }
        catch (System.Exception e) {
            // A corrupt index would otherwise take every profile down with it.
            Debug.LogError($"[SaveSystem] Could not read {IndexFile}: {e.Message}");
            return new ProfileIndex();
        }
    }

    public static void SaveIndex(ProfileIndex index) {
        Write(PathTo(IndexFile), JsonUtility.ToJson(index, true));
    }

    // RUNS

    public static bool HasRun(int profileId) => File.Exists(PathTo(RunFileName(profileId)));

    public static RunSave LoadRun(int profileId) {
        string path = PathTo(RunFileName(profileId));
        if (!File.Exists(path)) return null;

        try {
            return JsonUtility.FromJson<RunSave>(File.ReadAllText(path));
        }
        catch (System.Exception e) {
            Debug.LogError($"[SaveSystem] Could not read run {profileId}: {e.Message}");
            return null;
        }
    }

    public static void SaveRun(int profileId, RunSave run) {
        Write(PathTo(RunFileName(profileId)), JsonUtility.ToJson(run, true));
    }

    public static void DeleteRun(int profileId) {
        string path = PathTo(RunFileName(profileId));
        if (File.Exists(path)) File.Delete(path);
    }

    public static int UsedCount() {
        int n = 0;
        for (int i = 0; i < SlotCount; i++) if (HasRun(i)) n++;
        return n;
    }

    // Slots are positions: saves sit at 1,2,3 with no holes. Also repairs a hand-edited folder.
    public static void Compact() {
        ProfileIndex index = LoadIndex();

        int oldLast = index.lastUsedProfile;
        int newLast = -1;

        var kept = new System.Collections.Generic.List<ProfileEntry>();
        int write = 0;

        for (int read = 0; read < SlotCount; read++) {
            if (!HasRun(read)) continue;

            ProfileEntry entry = index.Get(read) ?? new ProfileEntry();

            if (read != write) MoveRun(read, write);
            if (read == oldLast) newLast = write;

            entry.profileId = write;
            entry.slotUsed  = true;
            entry.saveFile  = RunFileName(write);

            kept.Add(entry);
            write++;
        }

        index.profiles = kept;
        index.lastUsedProfile = newLast;

        SaveIndex(index);
    }

    private static void MoveRun(int from, int to) {
        string src = PathTo(RunFileName(from));
        string dst = PathTo(RunFileName(to));

        try {
            if (File.Exists(dst)) File.Delete(dst);
            File.Move(src, dst);
        }
        catch (System.Exception e) {
            Debug.LogError($"[SaveSystem] Could not move slot {from} to {to}: {e.Message}");
        }
    }

    // Temp file then swap, so a crash mid-write leaves the old save intact.
    private static void Write(string path, string json) {
        try {
            string temp = path + ".tmp";
            File.WriteAllText(temp, json);

            if (File.Exists(path)) File.Delete(path);
            File.Move(temp, path);
        }
        catch (System.Exception e) {
            Debug.LogError($"[SaveSystem] Could not write {path}: {e.Message}");
        }
    }
}
