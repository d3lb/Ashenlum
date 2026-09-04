using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckPoint : Interactable {
    [SerializeField] private string checkpointEntranceId;
    [SerializeField] private RestWave restWavePrefab;

    // Only used when there is no wave prefab to wait on.
    [SerializeField] private float freezeTime = 1.5f;

    private bool resting;

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

    protected override bool CanInteract => !resting;

    protected override string PromptVerb => Discovered ? "Rest" : "Discover";

    // Added regardless of discovery - it can be lit while standing here.
    protected override void OnPlayerEnter() => nearby.Add(this);
    protected override void OnPlayerExit()  => nearby.Remove(this);

    protected override void Interact() {
        GameManager.Instance.SetCheckpoint(checkpointEntranceId);

        if (!Discovered) {
            GameManager.Instance.activeRun.openedCheckpoints.Add(checkpointEntranceId);
            GameManager.Instance.MarkDirty();
            // First visit only lights it. Discovery animation goes here.
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

        if (!resting) return;

        resting = false;
        restingCount--;
        TimeManager.Release(this);
    }
}
