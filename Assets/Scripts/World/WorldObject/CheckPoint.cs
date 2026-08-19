using System.Collections;
using UnityEngine;

public class CheckPoint : Interactable
{
    [SerializeField] private string checkpointEntranceId;
    [SerializeField] private RestWave restWavePrefab;

    // Only used when there is no wave prefab to wait on.
    [SerializeField] private float freezeTime = 1.5f;

    private bool resting;

    public string CheckpointEntranceId => checkpointEntranceId;

    private bool Discovered =>
        GameManager.Instance.activeRun.openedCheckpoints.Contains(checkpointEntranceId);

    protected override bool CanInteract => !resting;

    protected override string PromptVerb => Discovered ? "Rest" : "Discover";

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

        StartCoroutine(Rest());
    }

    private IEnumerator Rest()
    {
        resting = true;
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
    }

    // Never leave the game frozen if this is torn down mid-rest.
    private void OnDisable()
    {
        if (!resting) return;

        resting = false;
        TimeManager.Release(this);
    }
}
