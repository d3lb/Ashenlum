using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    private bool isPlayerInRange = false;

    void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            SaveProgress();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            Debug.Log("Physics Trigger: Player detected.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
        }
    }

    private void SaveProgress()
    {
        PlayerRespawn.checkpointPosition = transform.position;
        PlayerRespawn.hasCheckpoint = true;
        Debug.Log("Input Trigger: Progress Saved!");
    }
}