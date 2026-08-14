using UnityEngine;

public class CheckPoint : Interactable
{
    [SerializeField] private string checkpointEntranceId;

    public string CheckpointEntranceId => checkpointEntranceId;

    private bool Discovered =>
        GameManager.Instance.activeRun.openedCheckpoints.Contains(checkpointEntranceId);

    protected override string PromptVerb => Discovered ? "Rest" : "Discover";

    protected override void Interact()
    {
        if (!Discovered)
        {
            GameManager.Instance.activeRun.openedCheckpoints.Add(checkpointEntranceId);
            // First visit - discovery animation goes here.
        }

        GameManager.Instance.SetCheckpoint(checkpointEntranceId);
    }
}
