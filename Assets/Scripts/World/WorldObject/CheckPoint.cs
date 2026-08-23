using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckPoint : Interactable
{
    [SerializeField] private string checkpointEntranceId;
    [SerializeField] private RestWave restWavePrefab;

    // Only used when there is no wave prefab to wait on.
    [SerializeField] private float freezeTime = 1.5f;

    private bool resting;

    // Counted rather than a bool: the wave is a moment nothing else may open over,
    // and two checkpoints resting at once must not clear each other's flag.
    private static int restingCount;
    public static bool Resting => restingCount > 0;

    // Statics outlive the scene. A count stuck above zero would lock every panel shut,
    // so it is cleared alongside the freeze stack on every load.
    public static void ClearResting() => restingCount = 0;

    // Standing at a lit checkpoint is what unlocks changing your loadout.
    private static readonly HashSet<CheckPoint> nearby = new();

    public static bool PlayerAtCheckpoint
    {
        get
        {
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

    // Added regardless of discovery, since it can be lit while standing here.
    protected override void OnPlayerEnter() => nearby.Add(this);
    protected override void OnPlayerExit()  => nearby.Remove(this);

    protected override void Interact()
    {
        GameManager.Instance.SetCheckpoint(checkpointEntranceId);

        if (!Discovered)
        {
            GameManager.Instance.activeRun.openedCheckpoints.Add(checkpointEntranceId);
            GameManager.Instance.MarkDirty();
            // First visit only lights it. Discovery animation goes here.
            return;
        }

        BeginRest();
    }

    public void BeginRest()
    {
        if (resting) return;
        StartCoroutine(Rest());
    }

    private IEnumerator Rest()
    {
        resting = true;
        restingCount++;
        TimeManager.Freeze(this);

        PlayerHealth player = FindFirstObjectByType<PlayerHealth>();

        // The wave comes off the player, not the checkpoint - it is their rest.
        Vector3 origin = player != null ? player.transform.position : transform.position;

        if (player != null) player.Heal(player.MaxHP);

        WorldReset.ResetAll();

        // Held for the whole wave; the wave destroying itself is the cue to release.
        if (restWavePrefab != null)
        {
            RestWave wave = Instantiate(restWavePrefab, origin, Quaternion.identity);
            while (wave != null) yield return null;
        }
        else
        {
            yield return new WaitForSecondsRealtime(freezeTime);
        }

        TimeManager.Release(this);
        resting = false;
        restingCount--;

        // Sitting down is the rest. The menu is what you do while sitting there.
        if (RestPointUI.Instance != null) RestPointUI.Instance.Open(this);
    }

    // Never leave the game frozen if this is torn down mid-rest.
    private void OnDisable()
    {
        nearby.Remove(this);

        if (!resting) return;

        resting = false;
        restingCount--;
        TimeManager.Release(this);
    }
}
