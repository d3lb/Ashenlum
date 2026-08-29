using System.Collections;
using UnityEngine;

// One move. Add a subclass, drop it on the King, and the brain picks it up automatically.
//
// Far thinner than SecretaryBirdAttack because the King never repositions: no dash,
// no hop, no feint. Everything he does is light appearing somewhere in the room.
public abstract class KingAttack : MonoBehaviour
{
    [Header("Selection")]
    [SerializeField] private string label = "";
    [SerializeField] private int weight = 1;
    [SerializeField] private int minPhase = 1;
    [SerializeField] private int maxPhase = 0;   // 0 = no upper limit

    [Header("Windows")]
    [SerializeField] private float recovery = 0.6f;
    [SerializeField] private float timeout = 8f;

    // Attacks the brain fires deliberately, never through the random draw: the
    // transition burst and the aggression punish.
    [SerializeField] private bool scripted = false;

    // Runs at the same time as another attack instead of taking its own turn. Phase 2
    // presses by overlapping, so it needs to know which moves are safe to double up.
    [SerializeField] private bool canOverlap = true;

    protected KingState State { get; private set; }
    protected KingBrain Brain { get; private set; }

    public int Weight => Mathf.Max(1, weight);
    public float Recovery => recovery;
    public float Timeout => timeout;
    public bool Scripted => scripted;
    public bool CanOverlap => canOverlap;

    public virtual string DisplayName =>
        string.IsNullOrEmpty(label) ? GetType().Name : label;

    protected virtual void Awake()
    {
        State = GetComponent<KingState>();
        Brain = GetComponent<KingBrain>();
    }

    public virtual bool CanUse(int phase) =>
        !scripted && phase >= minPhase && (maxPhase <= 0 || phase <= maxPhase);

    public abstract IEnumerator Act(Transform player);

    // Telegraph lengths scale per phase, so one number retunes readability everywhere.
    protected float Telegraph(float seconds) => seconds * Brain.TelegraphScale;
}
