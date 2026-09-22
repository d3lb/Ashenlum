using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CheckPoint : Interactable {
    [SerializeField] private string checkpointEntranceId;
    [SerializeField] private RestWave restWavePrefab;

    // Only used when there is no wave prefab to wait on.
    [SerializeField] private float freezeTime = 1.5f;

    // Two states: Dormant holds the unlit image, Discovering runs the clip and holds the lit
    // one. Turn Loop Time off on the clip or it will never settle on the last frame.
    [Header("Discovery")]
    [SerializeField] private Animator animator;
    [SerializeField] private string discoveredBool = "Discovered";

    // Must match the state name in the Animator window, not the clip's file name.
    [SerializeField] private string discoveredState = "Discovering";

    [SerializeField] private Light2D glow;
    [SerializeField] private float dormantIntensity;
    [SerializeField] private float litIntensity = 1f;

    // Scaled time, so the light and the clip stay in step.
    [SerializeField] private float riseDelay = 0.1f;
    [SerializeField] private float riseTime = 0.6f;

    private bool resting;
    private bool discovering;

    // Counted, so two checkpoints resting at once cannot clear each other's flag.
    private static int restingCount;
    public static bool Resting => restingCount > 0;

    // Statics outlive the scene, and a stuck count would lock every panel shut.
    public static void ClearResting() => restingCount = 0;

    private static readonly HashSet<CheckPoint> nearby = new();

    public static bool PlayerAtCheckpoint {
        get {
            foreach (CheckPoint c in nearby)
                if (c != null && c.Discovered) return true;

            return false;
        }
    }

    public string CheckpointEntranceId => checkpointEntranceId;

    private bool Discovered =>
        GameManager.Instance.activeRun.openedCheckpoints.Contains(checkpointEntranceId);

    protected override bool CanInteract => !resting && !discovering;

    protected override string PromptVerb => Discovered ? "Rest" : "Discover";

    // Start, not Awake: GameManager is up by then, so the saved state can be read.
    private void Start() => Apply(true);

    // instant skips the clip and the rise. Without it, re-entering a scene would play the
    // discovery again on a checkpoint that was lit hours ago.
    private void Apply(bool instant) {
        bool lit = Discovered;

        if (animator != null) {
            animator.SetBool(discoveredBool, lit);

            // Dropped on the last frame of the clip. Fast-forwarding with a big Update does not
            // reliably resolve a transition, which is why the animation was replaying on entry.
            // Update(0) flushes it now, so no frame of the unlit state is ever shown.
            if (instant && lit) {
                animator.Play(discoveredState, 0, 1f);
                animator.Update(0f);
            }
        }

        if (instant || !lit) {
            if (glow != null) glow.intensity = lit ? litIntensity : dormantIntensity;
            return;
        }

        StartCoroutine(Rise());
    }

    // Also the gate on CanInteract, so the clip cannot be cut short by a second press.
    private IEnumerator Rise() {
        discovering = true;

        if (glow != null) glow.intensity = dormantIntensity;

        for (float t = 0f; t < riseDelay; t += Time.deltaTime) yield return null;

        for (float t = 0f; t < riseTime; t += Time.deltaTime) {
            if (glow != null) glow.intensity = Mathf.Lerp(dormantIntensity, litIntensity, t / riseTime);
            yield return null;
        }

        if (glow != null) glow.intensity = litIntensity;
        discovering = false;
    }

    // Added regardless of discovery - it can be lit while standing here.
    protected override void OnPlayerEnter() => nearby.Add(this);
    protected override void OnPlayerExit()  => nearby.Remove(this);

    protected override void Interact() {
        GameManager.Instance.SetCheckpoint(checkpointEntranceId);

        if (!Discovered) {
            GameManager.Instance.activeRun.openedCheckpoints.Add(checkpointEntranceId);
            GameManager.Instance.MarkDirty();

            // First visit only lights it. Resting is the second press.
            Apply(false);
            return;
        }

        BeginRest();
    }

    public void BeginRest() {
        if (resting) return;
        StartCoroutine(Rest());
    }

    private IEnumerator Rest() {
        resting = true;
        restingCount++;
        TimeManager.Freeze(this);

        PlayerHealth player = FindFirstObjectByType<PlayerHealth>();

        Vector3 origin = player != null ? player.transform.position : transform.position;

        if (player != null) player.Heal(player.MaxHP);

        WorldReset.ResetAll();

        // The wave destroying itself is the cue to release.
        if (restWavePrefab != null) {
            RestWave wave = Instantiate(restWavePrefab, origin, Quaternion.identity);
            while (wave != null) yield return null;
        }
        else {
            yield return new WaitForSecondsRealtime(freezeTime);
        }

        TimeManager.Release(this);
        resting = false;
        restingCount--;

        // Sitting down is the rest; the menu comes after.
        if (RestPointUI.Instance != null) RestPointUI.Instance.Open(this);
    }

    // Torn down mid-rest must not leave the game frozen.
    private void OnDisable() {
        nearby.Remove(this);

        // The coroutine dies with the object, so the light would stop wherever it was.
        if (discovering) {
            discovering = false;
            if (glow != null) glow.intensity = litIntensity;
        }

        if (!resting) return;

        resting = false;
        restingCount--;
        TimeManager.Release(this);
    }
}
