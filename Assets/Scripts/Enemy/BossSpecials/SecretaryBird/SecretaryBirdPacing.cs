using UnityEngine;

[System.Serializable]
public class PhaseTuning {
    public string name = "Phase";
    public float idleBeat = 0.9f;
    public float telegraphScale = 1.4f;
    public float recoveryScale = 1f;
    public float speedScale = 0.9f;

    [Header("Reposition feint")]
    [Range(0, 2)] public int maxFeints = 0;
    [Range(0f, 1f)] public float feintChance = 0f;
}

public class SecretaryBirdPacing : MonoBehaviour {
    [SerializeField]
    private PhaseTuning[] phases = {
        new PhaseTuning { name = "1 - Teach",  idleBeat = 0.9f, telegraphScale = 1.4f,
                          recoveryScale = 1f,    speedScale = 0.9f, maxFeints = 0, feintChance = 0f },

        new PhaseTuning { name = "2 - Press",  idleBeat = 0.5f, telegraphScale = 1f,
                          recoveryScale = 0.75f, speedScale = 1f,   maxFeints = 1, feintChance = 0.4f },

        new PhaseTuning { name = "3 - Panic",  idleBeat = 0.2f, telegraphScale = 0.8f,
                          recoveryScale = 0.55f, speedScale = 1.1f, maxFeints = 2, feintChance = 0.6f },
    };

    private static readonly PhaseTuning fallback = new PhaseTuning();

    public PhaseTuning For(int phase) {
        if (phases == null || phases.Length == 0) return fallback;
        return phases[Mathf.Clamp(phase - 1, 0, phases.Length - 1)] ?? fallback;
    }
}
