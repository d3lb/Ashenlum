using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    private bool isPlayerInRange;

    private void Update()
    {
        if (
            isPlayerInRange &&
            Input.GetKeyDown(KeyCode.E)
        )
        {
            SaveProgress();
        }
    }

    private void OnTriggerEnter2D(
        Collider2D other
    )
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
        }
    }

    private void OnTriggerExit2D(
        Collider2D other
    )
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
        }
    }

    private void SaveProgress()
    {
        GameManager.Instance
            .SetCheckpoint(transform.position);

        Debug.Log("Checkpoint Saved");
    }
}