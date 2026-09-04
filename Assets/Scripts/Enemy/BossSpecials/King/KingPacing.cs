using UnityEngine;

[System.Serializable]
public class KingPhaseTuning {
    public string name = "Phase";

    public float idleBeat = 1.1f;
    public float telegraphScale = 1.4f;
    public float recoveryScale = 1f;

    // Later phases overlap attacks rather than only shortening the gaps.
    [Range(0f, 1f)] public float doubleUpChance = 0f;
}

public class KingPacing : MonoBehaviour {
    [SerializeField]
    private KingPhaseTuning[] phases = {
        new KingPhaseTuning { name = "1 - Judgement", idleBeat = 1.1f, telegraphScale = 1.4f,
                              recoveryScale = 1f,    doubleUpChance = 0f },

        new KingPhaseTuning { name = "2 - Correction", idleBeat = 0.8f, telegraphScale = 1.1f,
                              recoveryScale = 0.8f,  doubleUpChance = 0.5f },

        new KingPhaseTuning { name = "3 - The Crack",  idleBeat = 0.5f, telegraphScale = 0.9f,
                              recoveryScale = 0.6f,  doubleUpChance = 0.75f },
    };

    private static readonly KingPhaseTuning fallback = new KingPhaseTuning();

    public KingPhaseTuning For(int phase) {
        if (phases == null || phases.Length == 0) return fallback;
        return phases[Mathf.Clamp(phase - 1, 0, phases.Length - 1)] ?? fallback;
    }
}
