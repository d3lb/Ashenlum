using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    private PlayerInput input;
    [SerializeField] private string checkpointEntranceId;
    public string CheckpointEntranceId => checkpointEntranceId;
    private bool isPlayerInRange;

    private void Update()
    {
        if (isPlayerInRange && input.InteractPressed)
        {
            SaveProgress();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            input = other.GetComponent<PlayerInput>();
            isPlayerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            input = null;
        }
    }

    private void SaveProgress()
    {
        GameManager.Instance.SetCheckpoint(checkpointEntranceId);

        Debug.Log("Checkpoint Saved");
    }
}