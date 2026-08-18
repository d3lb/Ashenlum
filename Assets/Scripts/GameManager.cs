using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{

    public static GameManager Instance;
    // Active Run
    // NonSerialized on purpose: this is runtime state. Left serialized, Unity bakes a copy
    // into Managers.prefab and those stale values silently win over the C# defaults - which
    // is how currentHp stayed pinned at 100 no matter what the player's maxHp was set to.
    [System.NonSerialized] public GameRunProfile activeRun = new GameRunProfile();

    // Turns saved ids back into assets. Without it a loaded run keeps its lumens and
    // its progress but forgets every talisman, bundle and ability it owned.
    [SerializeField] private GameAssetDatabase assetDatabase;

    // Default Spawn Scene
    private string startingScene = "Start";

    // The menu is not part of a run. Recording it as currentArea is how Continue ends
    // up loading you straight back into the menu you just left.
    [SerializeField] private string menuScene = "MainMenu";

    // SAVING
    public int CurrentProfileId { get; private set; } = -1;

    // Writes are marked, not performed. Picking up twenty lumens should cost one file
    // write, not twenty.
    [SerializeField] private float saveInterval = 1f;

    private bool  saveDirty;
    private float nextSaveAt;
    private float sessionStart;

    // Pending Transitions
    private string pendingCheckpointScene;
    private bool pendingFadeIn;

    public System.Action OnSceneReady;

    public int CurrentLumens => activeRun.lumens;


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

    // No slot is chosen: a new run always takes the next free position.
    public bool StartNewGame()
    {
        SaveSystem.Compact();

        int profileId = SaveSystem.UsedCount();
        if (profileId >= SaveSystem.SlotCount) return false;

        activeRun = new GameRunProfile();

        CurrentProfileId = profileId;
        sessionStart = Time.unscaledTime;

        ProfileIndex index = SaveSystem.LoadIndex();
        ProfileEntry entry = index.Get(profileId) ?? NewEntry(index, profileId);

        entry.slotUsed = true;
        entry.playTime = 0f;
        entry.deaths   = 0;

        index.lastUsedProfile = profileId;
        SaveSystem.SaveIndex(index);
        SaveSystem.SaveRun(profileId, activeRun.ToSave());

        GoToScene(startingScene, "");
        return true;
    }

    // Picks up where the last played profile left off. False if there is nothing to
    // continue, so the menu can grey the button out instead of loading an empty run.
    public bool ContinueGame()
    {
        ProfileIndex index = SaveSystem.LoadIndex();

        if (index.lastUsedProfile >= 0 && LoadProfile(index.lastUsedProfile)) return true;

        // The index lost track, or predates it existing. A real save file still beats
        // refusing to continue.
        for (int i = 0; i < SaveSystem.SlotCount; i++)
            if (SaveSystem.HasRun(i)) return LoadProfile(i);

        return false;
    }

    public bool LoadProfile(int profileId)
    {
        if (profileId < 0) return false;

        RunSave save = SaveSystem.LoadRun(profileId);
        if (save == null) return false;

        activeRun = new GameRunProfile();
        activeRun.ApplySave(save, assetDatabase);

        CurrentProfileId = profileId;
        sessionStart = Time.unscaledTime;

        ProfileIndex index = SaveSystem.LoadIndex();
        index.lastUsedProfile = profileId;
        SaveSystem.SaveIndex(index);

        GoToScene(activeRun.currentArea, activeRun.targetEntranceId);
        return true;
    }

    public void DeleteProfile(int profileId)
    {
        // The run being played could be renumbered out from under us, so stop tracking
        // it. In practice this only ever runs from the menu, where nothing is active.
        if (CurrentProfileId == profileId) CurrentProfileId = -1;

        SaveSystem.DeleteRun(profileId);
        SaveSystem.Compact();
    }

    private void CountDeath()
    {
        if (CurrentProfileId < 0) return;

        ProfileIndex index = SaveSystem.LoadIndex();
        ProfileEntry entry = index.Get(CurrentProfileId) ?? NewEntry(index, CurrentProfileId);

        entry.deaths++;
        SaveSystem.SaveIndex(index);
    }

    private ProfileEntry NewEntry(ProfileIndex index, int profileId)
    {
        ProfileEntry entry = new ProfileEntry
        {
            profileId = profileId,
            saveFile  = SaveSystem.RunFileName(profileId)
        };

        index.profiles.Add(entry);
        return entry;
    }

    // Call after anything worth keeping. Cheap - it only sets a flag.
    public void MarkDirty() => saveDirty = true;

    public void SaveNow()
    {
        if (CurrentProfileId < 0) return;

        saveDirty = false;
        SaveSystem.SaveRun(CurrentProfileId, activeRun.ToSave());

        ProfileIndex index = SaveSystem.LoadIndex();
        ProfileEntry entry = index.Get(CurrentProfileId) ?? NewEntry(index, CurrentProfileId);

        entry.slotUsed  = true;
        entry.playTime += Time.unscaledTime - sessionStart;
        sessionStart    = Time.unscaledTime;

        index.lastUsedProfile = CurrentProfileId;
        SaveSystem.SaveIndex(index);
    }

    private void Update()
    {
        if (!saveDirty || Time.unscaledTime < nextSaveAt) return;

        nextSaveAt = Time.unscaledTime + saveInterval;
        SaveNow();
    }

    // Quitting must never be a way to undo the last few seconds.
    private void OnApplicationQuit() => SaveNow();

    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveNow();
    }

    public void GoToScene(string sceneName, string entranceId)
    {
        activeRun.targetEntranceId = entranceId;
        activeRun.isTransitioningScenes = true;

        if (sceneName == menuScene)
        {
            // Flush the run with its real area, then stop tracking it.
            SaveNow();
            CurrentProfileId = -1;
        }
        else
        {
            // Written before the save: OnSceneLoaded sets this too, but that happens
            // after the file is on disk, so a save here would record the scene left.
            activeRun.currentArea = sceneName;
            SaveNow();
        }

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
        if (scene.name != menuScene) activeRun.currentArea = scene.name;
        TimeManager.ReleaseAll();
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
        activeRun.checkpointScene = SceneManager.GetActiveScene().name;
        activeRun.checkpointEntranceId = entranceId;
        activeRun.hasCheckpoint = true;

        MarkDirty();
    }

    public void GoToCheckpoint()
    {
        if (!activeRun.hasCheckpoint)
            return;

        activeRun.currentHp = activeRun.maxHp;

        pendingCheckpointScene = activeRun.checkpointScene;
        activeRun.currentArea  = activeRun.checkpointScene;

        activeRun.temporaryRemoved.Clear();

        StartCoroutine(LoadSceneRoutine(activeRun.checkpointScene));
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
                if (entrance.CheckpointEntranceId == activeRun.checkpointEntranceId)
                {
                    player.transform.position = entrance.transform.position;
                    break;
                }
            }
        }

        yield return SceneFader.Instance.FadeIn(0.4f);
    }


    public bool HasCheckpoint()
    {
        return activeRun.hasCheckpoint;
    }

    public string GetCheckpointEntranceId()
    {
        return activeRun.checkpointEntranceId;
    }

    // WALLS

    public void RegisterBrokenWall(string wallID)
    {
        activeRun.brokenWalls.Add(wallID);
        MarkDirty();
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
        MarkDirty();
    }

    public void TakeLumens(int amount)
    {
        activeRun.lumens -= amount;
        OnLumensChanged?.Invoke(activeRun.lumens);
        MarkDirty();
    }

    public bool UseBundle(LumenBundle bundle)
    {
        if (bundle == null || !activeRun.ConsumeBundle(bundle)) return false;

        AddLumens(bundle.value);
        return true;
    }

    // PLAYER DEATH
    public void PlayerDied(float respawnDelay)
    {
        activeRun.temporaryRemoved.Clear();

        DropShade();

        CountDeath();
        SaveNow();

        StartCoroutine(RespawnRoutine(respawnDelay));
    }

    // Everything you were carrying stays behind on the last ground you stood on. Anchored
    // to the player rather than the death spot so a spike death does not leave the pile
    // somewhere you would have to kill yourself to collect.
    private void DropShade()
    {
        // Dying replaces the pile. Whatever was still out there is gone - that is the
        // cost, and it applies even when you die broke and leave nothing new behind.
        activeRun.droppedLumens = 0;
        activeRun.dropScene     = null;

        if (activeRun.lumens <= 0) return;

        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();

        // No player to read a safe spot from - keep the lumens rather than strand them.
        if (player == null) return;

        activeRun.droppedLumens = activeRun.lumens;
        activeRun.dropScene     = activeRun.currentArea;
        activeRun.dropPosition  = player.LastSafeGround;

        activeRun.lumens = 0;
        OnLumensChanged?.Invoke(0);
    }

    // Called by the shade once it has been cracked open. Only clears the record - the
    // pickups it spawned carry the lumens and pay out as the player touches them.
    public void CollectShade()
    {
        if (!activeRun.HasShade) return;

        activeRun.droppedLumens = 0;
        activeRun.dropScene     = null;
    }

    private IEnumerator RespawnRoutine(float respawnDelay)
    {
        yield return new WaitForSecondsRealtime(respawnDelay);

        if (activeRun.hasCheckpoint)
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