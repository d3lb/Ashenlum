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

    protected KingState State { get; private set; }
    protected KingBrain Brain { get; private set; }

    public int Weight => Mathf.Max(1, weight);
    public float Recovery => recovery;
    public float Timeout => timeout;

    // What a move IS, not how it is tuned. A subclass either is fired only by the brain
    // or it is not, and that never changes per instance.
    public virtual bool Scripted => false;
    public virtual bool CanOverlap => true;

    public virtual string DisplayName =>
        string.IsNullOrEmpty(label) ? GetType().Name : label;

    protected virtual void Awake()
    {
        State = GetComponent<KingState>();
        Brain = GetComponent<KingBrain>();

        // The brain finds its attacks with GetComponents, so one sitting on a child
        // is invisible to it and would never fire. Say so instead of doing nothing.
        if (Brain == null)
            Debug.LogError($"[{GetType().Name}] '{name}' is not on the same object as " +
                           "KingBrain, so it will never be used.", this);
    }

    public virtual bool CanUse(int phase) =>
        !Scripted && phase >= minPhase && (maxPhase <= 0 || phase <= maxPhase);

    public abstract IEnumerator Act(Transform player);

    // Telegraph lengths scale per phase, so one number retunes readability everywhere.
    protected float Telegraph(float seconds) =>
        Brain != null ? seconds * Brain.TelegraphScale : seconds;

    // Damage, layer and look all come from the brain, so an attack only decides where
    // and how big.
    protected KingLight SpawnLight(Vector2 position, Vector2 size, float angle,
                                   float telegraphTime, float activeTime)
    {
        return KingLight.Spawn(Brain, position, size, angle, telegraphTime, activeTime);
    }
}
