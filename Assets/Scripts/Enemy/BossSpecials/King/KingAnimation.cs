using UnityEngine;
using UnityEngine.Rendering.Universal;

// He never leaves the throne, so this is the whole of his animation: an arm that rises to cast
// and a halo that dims as the phases pass.
public class KingAnimation : MonoBehaviour {
    [SerializeField] private Animator animator;
    [SerializeField] private KingState state;

    // A moment at the start of the windup, not the whole attack - the telegraph outlasts him.
    [Header("Cast")]
    [SerializeField] private float castTime = 0.7f;

    [Header("Staff")]
    [SerializeField] private Light2D staffLight;
    [SerializeField] private float staffIntensity = 2f;

    // Peaks while the arm is held at the top, so the glow and the pose land together.
    [SerializeField] private AnimationCurve staffCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.45f, 1f),
        new Keyframe(0.7f, 1f),
        new Keyframe(1f, 0f));

    [Header("Halo")]
    [SerializeField] private SpriteRenderer halo;

    // One per phase, in order. A shorter array just holds its last entry.
    [SerializeField] private Sprite[] haloByPhase;
    [SerializeField] private Light2D haloLight;
    [SerializeField] private float[] haloIntensityByPhase;

    private int shownPhase = -1;
    private float castStart = float.NegativeInfinity;
    private KingState.KingStateType lastState;

    private void Awake() {
        if (state == null) state = GetComponent<KingState>();

        if (animator == null)
            Debug.LogError($"[KingAnimation] '{name}' has no Animator.", this);
    }

    private void Update() {
        // Attacking, not Windup: the Windup state exists in the enum but the brain never sets
        // it. RunAttacks raises Attacking once per beat, which is the cue.
        bool entered = state.CurrentState == KingState.KingStateType.Attacking
                    && lastState != KingState.KingStateType.Attacking;

        lastState = state.CurrentState;
        if (entered) castStart = Time.time;

        float t = (Time.time - castStart) / Mathf.Max(0.01f, castTime);
        bool casting = t >= 0f && t <= 1f;

        if (animator != null) animator.SetBool("IsCasting", casting);

        if (staffLight != null)
            staffLight.intensity = casting ? staffIntensity * staffCurve.Evaluate(t) : 0f;

        if (shownPhase != state.Phase) ApplyPhase();
    }

    private void ApplyPhase() {
        shownPhase = state.Phase;

        if (halo != null && haloByPhase != null && haloByPhase.Length > 0)
            halo.sprite = haloByPhase[Mathf.Clamp(state.Phase - 1, 0, haloByPhase.Length - 1)];

        if (haloLight != null && haloIntensityByPhase != null && haloIntensityByPhase.Length > 0)
            haloLight.intensity =
                haloIntensityByPhase[Mathf.Clamp(state.Phase - 1, 0, haloIntensityByPhase.Length - 1)];
    }
}
