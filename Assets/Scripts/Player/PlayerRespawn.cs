using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    private void Start()
    {
        if (GameManager.Instance.HasCheckpoint())
        {
            transform.position =
                GameManager.Instance
                .GetCheckpointPosition();
        }
    }
}