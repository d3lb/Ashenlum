using UnityEngine;

// A new sound needs an enum entry and a bank entry.
public enum SoundId {
    None = 0,

    Footstep,
    Jump,
    DoubleJump,
    Land,
    Dash,
    WallSlide,

    Attack,
    HitDealt,
    Kill,

    HitTaken,
    Death,

    Pickup,
    Heal,
    Interact,
    Rest,

    UIClick,
    UIBack,
}

[CreateAssetMenu(fileName = "Sound Bank", menuName = "Ashenlum/Sound Bank")]
public class SoundBank : ScriptableObject {
    [System.Serializable]
    public class Entry {
        public SoundId id;

        public AudioClip[] clips;

        [Range(0f, 1f)] public float volume = 1f;

        // Repeated identical samples machine-gun.
        [Range(0f, 0.5f)] public float pitchVariance = 0.08f;

        // One frame can ask several times: overlapping lights, a multi-hit swing.
        public float minInterval = 0.04f;
    }

    [SerializeField] private Entry[] entries;

    public Entry Find(SoundId id) {
        if (entries == null || id == SoundId.None) return null;

        foreach (Entry e in entries)
            if (e != null && e.id == id) return e;

        return null;
    }

#if UNITY_EDITOR
    private void OnValidate() {
        if (entries == null) return;

        for (int i = 0; i < entries.Length; i++) {
            if (entries[i] == null) continue;

            if (entries[i].clips == null || entries[i].clips.Length == 0)
                Debug.LogWarning($"[SoundBank] '{entries[i].id}' has no clips, so it is silent.", this);

            for (int j = i + 1; j < entries.Length; j++)
                if (entries[j] != null && entries[i].id == entries[j].id)
                    Debug.LogError($"[SoundBank] '{entries[i].id}' is listed twice. " +
                                   "The first one wins and the second never plays.", this);
        }
    }
#endif
}
