using System.Collections.Generic;
using UnityEngine;

// A checkpoint lives in a scene that is not loaded, so the travel list cannot find it
// by searching. Every checkpoint has to be listed here to be travelled to.
[CreateAssetMenu(fileName = "Checkpoint Directory", menuName = "Ashenlum/Checkpoint Directory")]
public class CheckpointDirectory : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        // Must match the Checkpoint Entrance Id on the checkpoint in the scene.
        public string id;
        public string displayName = "Rest Point";

        // Scene name exactly as it appears in Build Settings.
        public string scene;
    }

    [SerializeField] private Entry[] entries;

    public Entry Find(string id)
    {
        if (string.IsNullOrEmpty(id) || entries == null) return null;

        foreach (Entry entry in entries)
            if (entry != null && entry.id == id) return entry;

        return null;
    }

    // Listed in directory order, so the inspector controls how the travel menu reads.
    public List<Entry> Discovered(HashSet<string> openedIds)
    {
        List<Entry> found = new();
        if (entries == null || openedIds == null) return found;

        foreach (Entry entry in entries)
            if (entry != null && openedIds.Contains(entry.id)) found.Add(entry);

        return found;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (entries == null) return;

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] == null) continue;

            if (string.IsNullOrEmpty(entries[i].scene))
                Debug.LogWarning($"[CheckpointDirectory] '{entries[i].id}' has no scene, " +
                                 "so travelling to it will do nothing.", this);

            for (int j = i + 1; j < entries.Length; j++)
                if (entries[j] != null && entries[i].id == entries[j].id)
                    Debug.LogError($"[CheckpointDirectory] Two entries share the id " +
                                   $"'{entries[i].id}'. Travel will pick the wrong one.", this);
        }
    }
#endif
}
