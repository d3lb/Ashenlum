using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // Active Run 
    public GameRunProfile activeRun = new GameRunProfile();

    // Checkpoint
    private Vector2 checkpointPosition;
    private bool hasCheckpoint;


    private void Awake()
    {
        Application.targetFrameRate = 60;
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);

            activeRun.currentArea = SceneManager.GetActiveScene().name;

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        activeRun.currentArea = scene.name;
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
        activeRun.brokenWalls.Add(wallID);
    }

    public bool IsWallBroken(string wallID)
    {
        return activeRun.brokenWalls.Contains(wallID);
    }

    public System.Action<int> OnLumensChanged;

    public void AddLumens(int amount)
    {
        activeRun.lumens += amount;
        OnLumensChanged?.Invoke(activeRun.lumens);
    }

    public void TakeLumens(int amount)
    {
        activeRun.lumens -= amount;
        OnLumensChanged?.Invoke(activeRun.lumens);
    }
}