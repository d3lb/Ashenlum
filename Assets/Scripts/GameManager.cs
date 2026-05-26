using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // Checkpoint
    private Vector2 checkpointPosition;
    private bool hasCheckpoint;

    // Wall
    private HashSet<string> brokenWalls = new HashSet<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // CHECKPOINTS

    public void SetCheckpoint(Vector2 pos)
    {
        checkpointPosition = pos;
        hasCheckpoint = true;
    }

    public bool HasCheckpoint()
    {
        return hasCheckpoint;
    }

    public Vector2 GetCheckpointPosition()
    {
        return checkpointPosition;
    }

    // WALLS

    public void RegisterBrokenWall(string wallID)
    {
        brokenWalls.Add(wallID);
    }

    public bool IsWallBroken(string wallID)
    {
        return brokenWalls.Contains(wallID);
    }
}