using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    public static Vector2 checkpointPosition;
    public static bool hasCheckpoint = false;

    void Start()
    {
        if (hasCheckpoint)
        {
            transform.position = checkpointPosition;
        }
    }
}