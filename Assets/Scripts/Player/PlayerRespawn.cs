using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    private void Start()
    {
        // traveling through a door
        if (GameManager.Instance.activeRun.isTransitioningScenes)
        {
            return;
        }

        // loading a save file or dying
        if (GameManager.Instance.HasCheckpoint())
        {
            GameManager.Instance.GoToCheckpoint();
        }
    }
}