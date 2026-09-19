using UnityEngine;
using UnityEngine.Rendering.Universal;

// The fight's only progress read: the moon crosses the sun as he loses health, and the world
// dims with it. Not a health bar - nothing else in the game has one - just spectacle.
public class KingEclipse : MonoBehaviour {
    // One parent, every SpriteRenderer under it. The wash multiplies each renderer's own
    // colour, so variation authored inside a group survives instead of being flattened.
    [System.Serializable]
    public class Tint {
        public string label;
        public Transform root;

        // White leaves the group exactly as authored.
        public Color start = Color.white;
        public Color end = Color.white;

        [System.NonSerialized] public SpriteRenderer[] renderers;
        [System.NonSerialized] public Color[] bases;

        public void Collect() {
            if (root == null) return;

            renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            bases = new Color[renderers.Length];

            for (int i = 0; i < renderers.Length; i++) bases[i] = renderers[i].color;
        }

        public void Apply(float t) {
            if (renderers == null) return;

            Color wash = Color.Lerp(start, end, t);

            for (int i = 0; i < renderers.Length; i++) {
                if (renderers[i] == null) continue;

                renderers[i].color = bases[i] * wash;
            }
        }
    }

    [Header("Source")]
    [SerializeField] private KingHealth health;

    // He is destroyed on death and never rebuilt, so once he is beaten there is no health
    // left to read. The saved defeat flag is what holds the sky dark across scene loads.
    [SerializeField] private KingEncounter encounter;

    // Health fraction at which the eclipse is total. 0.33 lands it on the phase 3 transition.
    [SerializeField, Range(0f, 1f)] private float completeAt = 0.33f;

    // Damage arrives in lumps; this slides the moon instead of teleporting it.
    [SerializeField] private float smoothTime = 0.4f;

    [Header("Moon")]
    [SerializeField] private Transform moon;
    [SerializeField] private Transform moonTarget;

    // Sideways bow of the path. Negative arcs the other way.
    [SerializeField] private float arcHeight = 1.5f;

    [Header("Sky")]
    [SerializeField] private SpriteRenderer background;
    [SerializeField] private Color skyStart = new Color(1f, 0.62f, 0.25f);
    [SerializeField] private Color skyEnd = new Color(0.15f, 0.08f, 0.12f);

    [Header("Sun light")]
    [SerializeField] private Light2D sunLight;
    [SerializeField] private float lightStart = 3f;
    [SerializeField] private float lightEnd = 0.3f;

    [Header("Sun ring")]
    [SerializeField] private SpriteRenderer sunRing;
    [SerializeField] private Color ringStart = new Color(1f, 0.95f, 0.75f, 0f);
    [SerializeField] private Color ringEnd = new Color(1f, 0.85f, 0.6f, 1f);

    // Everything else: one parent per group, for the things there are too many of to drag.
    [Header("Tinted groups")]
    [SerializeField] private Tint[] tints;

    private Vector3 moonStart;
    private float shown;
    private float velocity;
    private bool previewing;

    public float Current => shown;

    // Sticky on purpose, so the value can be held and looked at - but anything that starts a
    // preview owns ending it, or health silently stops driving the eclipse for the session.
    public bool Previewing => previewing;

    // Debug only: holds the eclipse at a value so the whole range can be looked at without
    // having to fight him down to it.
    public void Preview(float t) {
        previewing = true;
        velocity = 0f;
        shown = Mathf.Clamp01(t);

        Apply(shown);
    }

    public void StopPreview() => previewing = false;

    private void Awake() {
        if (health == null) health = GetComponent<KingHealth>();
        if (encounter == null) encounter = FindFirstObjectByType<KingEncounter>();

        if (moon != null) moonStart = moon.position;

        // Once, not per frame: a cloud bank is a lot of renderers to go looking for.
        if (tints != null)
            foreach (Tint tint in tints) tint.Collect();
    }

    private void Start() {
        // Snapped, so loading into the fight does not play the whole eclipse on arrival.
        shown = Target;
        Apply(shown);
    }

    // 0 at full health, 1 once he is at completeAt or below.
    private float Target {
        get {
            if (encounter != null && encounter.AlreadyBeaten) return 1f;

            // Mid-death, before the flag is written: hold rather than snapping back to day.
            if (health == null) return shown;

            return Mathf.InverseLerp(1f, completeAt, health.Normalized);
        }
    }

    private void Update() {
        if (previewing) return;

        shown = Mathf.SmoothDamp(shown, Target, ref velocity, smoothTime);
        Apply(shown);
    }

    private void Apply(float t) {
        if (moon != null && moonTarget != null)
            moon.position = Arc(moonStart, moonTarget.position, arcHeight, t);

        if (background != null) background.color = Color.Lerp(skyStart, skyEnd, t);

        if (sunLight != null) sunLight.intensity = Mathf.Lerp(lightStart, lightEnd, t);

        if (sunRing != null) sunRing.color = Color.Lerp(ringStart, ringEnd, t);

        if (tints != null)
            foreach (Tint tint in tints) tint.Apply(t);
    }

    // Quadratic bezier with the control point pushed sideways off the midpoint, so a small
    // arcHeight bends the path without changing where it starts or ends.
    private static Vector3 Arc(Vector3 from, Vector3 to, float height, float t) {
        Vector3 mid = (from + to) * 0.5f;
        Vector3 direction = (to - from).normalized;
        Vector3 control = mid + new Vector3(-direction.y, direction.x, 0f) * height;

        float u = 1f - t;

        return (u * u * from) + (2f * u * t * control) + (t * t * to);
    }

    // The path, so arcHeight can be tuned without entering play mode.
    private void OnDrawGizmosSelected() {
        if (moon == null || moonTarget == null) return;

        Vector3 from = Application.isPlaying ? moonStart : moon.position;
        Vector3 previous = from;

        Gizmos.color = Color.cyan;

        for (int i = 1; i <= 24; i++) {
            Vector3 point = Arc(from, moonTarget.position, arcHeight, i / 24f);

            Gizmos.DrawLine(previous, point);
            previous = point;
        }

        Gizmos.DrawWireSphere(moonTarget.position, 0.3f);
    }
}
