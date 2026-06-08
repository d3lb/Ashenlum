using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{

    public static GameManager Instance;
    // Active Run 
    public GameRunProfile activeRun = new GameRunProfile();

    // Default Spawn Scene
    private string startingScene;

    // Checkpoint
    private string checkpointScene;
    private string checkpointEntranceId;
    private bool hasCheckpoint;

    private string pendingCheckpointScene;
    private bool pendingFadeIn;

    public System.Action OnSceneReady;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);

            activeRun.currentArea = SceneManager.GetActiveScene().name;
            startingScene = SceneManager.GetActiveScene().name;

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

    public void GoToScene(string sceneName, string entranceId)
    {
        activeRun.targetEntranceId = entranceId;
        activeRun.isTransitioningScenes = true;

        StartCoroutine(LoadSceneRoutine(sceneName));
    }


    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        yield return StartCoroutine(SceneFader.Instance.FadeOut(0f));
        pendingFadeIn = true;
        SceneManager.LoadScene(sceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        activeRun.currentArea = scene.name;
        StartCoroutine(NotifySceneReady());

        if (scene.name == pendingCheckpointScene)
        {
            StartCoroutine(FinishCheckpointTeleport());
            pendingCheckpointScene = null;
            return;
        }

        if (pendingFadeIn)
        {
            pendingFadeIn = false;
            StartCoroutine(SceneFader.Instance.FadeIn(0.4f));
        }
    }
    private IEnumerator NotifySceneReady()
    {
        yield return null;
        OnSceneReady?.Invoke();
    }

    // CHECKPOINTS
    public void SetCheckpoint(string entranceId)
    {
        checkpointScene = SceneManager.GetActiveScene().name;
        checkpointEntranceId = entranceId;
        hasCheckpoint = true;
    }

    public void GoToCheckpoint()
    {
        if (!hasCheckpoint) return;

        activeRun.currentHp = activeRun.maxHp;

        pendingCheckpointScene = checkpointScene;

        StartCoroutine(LoadSceneRoutine(checkpointScene));
    }

    private IEnumerator FinishCheckpointTeleport()
    {
        yield return null;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {

            CheckPoint[] entrances = FindObjectsByType<CheckPoint>(FindObjectsSortMode.None);

            foreach (var entrance in entrances)
            {
                if (entrance.CheckpointEntranceId == checkpointEntranceId)
                {
                    player.transform.position = entrance.transform.position;
                    break;
                }
            }
        }

        yield return SceneFader.Instance.FadeIn(0.4f);
    }

    public string GetCheckpointScene() => checkpointScene;

    public bool HasCheckpoint()
    {
        return hasCheckpoint;
    }

    public string GetCheckpointEntranceId()
    {
        return checkpointEntranceId;
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

    // PLAYER DEATH 
    public void PlayerDied(float respawnDelay)
    {
        StartCoroutine(RespawnRoutine(respawnDelay));
    }

    private IEnumerator RespawnRoutine(float respawnDelay)
    {
        yield return new WaitForSecondsRealtime(respawnDelay);

        if (hasCheckpoint)
        {
            GoToCheckpoint();
        }
        else
        {
            activeRun.currentHp = activeRun.maxHp;
            StartCoroutine(LoadSceneRoutine(startingScene));
        }
    }
}