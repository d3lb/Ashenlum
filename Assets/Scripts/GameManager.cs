using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// The one object that outlives every scene. Owns the active run, moves between scenes, and
// decides when the game is written to disk.
public class GameManager : MonoBehaviour {

    public static GameManager Instance;
    // Serialized, Unity bakes stale prefab values that beat the defaults.
    [System.NonSerialized] public GameRunProfile activeRun = new GameRunProfile();

    // Without it a loaded run forgets every talisman, bundle and ability.
    [SerializeField] private GameAssetDatabase assetDatabase;

    public GameAssetDatabase Assets => assetDatabase;

    // Default Spawn Scene
    private string startingScene = "Start";

    // Or Continue loads straight back into the menu.
    [SerializeField] private string menuScene = "MainMenu";

    // SAVING
    public int CurrentProfileId { get; private set; } = -1;

    // Marked, not written - twenty lumens should cost one file write.
    [SerializeField] private float saveInterval = 1f;

    private bool  saveDirty;
    private float nextSaveAt;
    private float sessionStart;

    // Pending Transitions
    private string pendingCheckpointScene;
    private bool pendingFadeIn;

    public System.Action OnSceneReady;

    public int CurrentLumens => activeRun.lumens;


    private void Awake() {
        Application.targetFrameRate = 60;
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);

            activeRun.currentArea = SceneManager.GetActiveScene().name;

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else {
            Destroy(gameObject);
        }

    }

    private void OnDestroy() {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // Left set, Instance points at a destroyed object: guarded callers see null while
        // the 44 unguarded activeRun reads throw instead.
        if (Instance == this) Instance = null;
    }

    // A new run always takes the next free slot.
    public bool StartNewGame() {
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
        entry.kills    = 0;

        index.lastUsedProfile = profileId;
        SaveSystem.SaveIndex(index);
        SaveSystem.SaveRun(profileId, activeRun.ToSave());

        GoToScene(startingScene, "");
        return true;
    }

    public bool ContinueGame() {
        ProfileIndex index = SaveSystem.LoadIndex();

        if (index.lastUsedProfile >= 0 && LoadProfile(index.lastUsedProfile)) return true;

        // A real save file beats a lost index entry.
        for (int i = 0; i < SaveSystem.SlotCount; i++)
            if (SaveSystem.HasRun(i)) return LoadProfile(i);

        return false;
    }

    public bool LoadProfile(int profileId) {
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

        Resume();
        return true;
    }

    private void SetResume(GameRunProfile.ResumeType type, string scene, string id) {
        activeRun.resumeType  = type;
        activeRun.resumeScene = scene;
        activeRun.resumeId    = id;
    }

    // Or quit-and-reload is a free full heal.
    private void Resume() {
        switch (activeRun.resumeType) {
            case GameRunProfile.ResumeType.Checkpoint when activeRun.hasCheckpoint:
                GoToCheckpoint(false);
                break;

            case GameRunProfile.ResumeType.Entrance when !string.IsNullOrEmpty(activeRun.resumeScene):
                GoToScene(activeRun.resumeScene, activeRun.resumeId);
                break;

            default:
                GoToScene(startingScene, "");
                break;
        }
    }

    public void DeleteProfile(int profileId) {
        // The active run may be renumbered underneath us.
        if (CurrentProfileId == profileId) CurrentProfileId = -1;

        SaveSystem.DeleteRun(profileId);
        SaveSystem.Compact();
    }

    private void CountDeath() {
        if (CurrentProfileId < 0) return;

        ProfileIndex index = SaveSystem.LoadIndex();
        ProfileEntry entry = index.Get(CurrentProfileId) ?? NewEntry(index, CurrentProfileId);

        entry.deaths++;
        SaveSystem.SaveIndex(index);
    }

    // Written by SaveNow, which already rewrites the index for playTime.
    private int pendingKills;

    public void CountKill() => pendingKills++;

    private ProfileEntry NewEntry(ProfileIndex index, int profileId) {
        ProfileEntry entry = new ProfileEntry {
            profileId = profileId,
            saveFile  = SaveSystem.RunFileName(profileId)
        };

        index.profiles.Add(entry);
        return entry;
    }

    public void MarkDirty() => saveDirty = true;

    public void SaveNow() {
        if (CurrentProfileId < 0) return;

        saveDirty = false;
        SaveSystem.SaveRun(CurrentProfileId, activeRun.ToSave());

        ProfileIndex index = SaveSystem.LoadIndex();
        ProfileEntry entry = index.Get(CurrentProfileId) ?? NewEntry(index, CurrentProfileId);

        entry.slotUsed  = true;
        entry.playTime += Time.unscaledTime - sessionStart;
        sessionStart    = Time.unscaledTime;

        entry.kills += pendingKills;
        pendingKills = 0;

        index.lastUsedProfile = CurrentProfileId;
        SaveSystem.SaveIndex(index);
    }

    private void Update() {
        if (!saveDirty || Time.unscaledTime < nextSaveAt) return;

        nextSaveAt = Time.unscaledTime + saveInterval;
        SaveNow();
    }

    // Quitting must not undo the last few seconds.
    private void OnApplicationQuit() => SaveNow();

    private void OnApplicationPause(bool paused) {
        if (paused) SaveNow();
    }

    public void GoToScene(string sceneName, string entranceId) {
        activeRun.targetEntranceId = entranceId;
        activeRun.isTransitioningScenes = true;

        if (sceneName == menuScene) {
            // Flush with the real area before untracking.
            SaveNow();
            CurrentProfileId = -1;
        }
        else {
            // OnSceneLoaded sets this too, but after the file is written.
            activeRun.currentArea = sceneName;

            // Newest action wins, even over an earlier rest.
            SetResume(GameRunProfile.ResumeType.Entrance, sceneName, entranceId);

            SaveNow();
        }

        StartCoroutine(LoadSceneRoutine(sceneName));
    }


    private IEnumerator LoadSceneRoutine(string sceneName) {
        yield return StartCoroutine(SceneFader.Instance.FadeOut(0f));
        pendingFadeIn = true;
        SceneManager.LoadScene(sceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if (scene.name != menuScene) activeRun.currentArea = scene.name;
        TimeManager.ReleaseAll();
        CheckPoint.ClearResting();
        StartCoroutine(NotifySceneReady());

        if (scene.name == pendingCheckpointScene) {
            // FinishCheckpointTeleport fades in itself.
            pendingFadeIn = false;
            StartCoroutine(FinishCheckpointTeleport());
            pendingCheckpointScene = null;
            return;
        }

        if (pendingFadeIn) {
            pendingFadeIn = false;
            StartCoroutine(SceneFader.Instance.FadeIn(0.4f));
        }
    }
    private IEnumerator NotifySceneReady() {
        yield return null;
        OnSceneReady?.Invoke();
    }

    // CHECKPOINTS
    public void SetCheckpoint(string entranceId) {
        activeRun.checkpointScene = SceneManager.GetActiveScene().name;
        activeRun.checkpointEntranceId = entranceId;
        activeRun.hasCheckpoint = true;

        SetResume(GameRunProfile.ResumeType.Checkpoint, activeRun.checkpointScene, entranceId);

        MarkDirty();
    }

    public void GoToCheckpoint(bool heal = true) {
        if (!activeRun.hasCheckpoint)
            return;

        // Dying and teleporting restore; loading does not.
        if (heal) activeRun.currentHp = activeRun.maxHp;

        pendingCheckpointScene = activeRun.checkpointScene;
        activeRun.currentArea  = activeRun.checkpointScene;

        activeRun.temporaryRemoved.Clear();

        StartCoroutine(LoadSceneRoutine(activeRun.checkpointScene));
    }

    private IEnumerator FinishCheckpointTeleport() {
        yield return null;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null) {

            CheckPoint[] entrances = FindObjectsByType<CheckPoint>(FindObjectsSortMode.None);

            foreach (var entrance in entrances) {
                if (entrance.CheckpointEntranceId == activeRun.checkpointEntranceId) {
                    player.transform.position = entrance.transform.position;
                    break;
                }
            }
        }

        yield return SceneFader.Instance.FadeIn(0.4f);
    }


    // SetCheckpoint only ever means this scene, so travel must name its destination.
    public void TravelToCheckpoint(string scene, string entranceId) {
        if (string.IsNullOrEmpty(scene) || string.IsNullOrEmpty(entranceId)) {
            Debug.LogError($"[GameManager] Travel to '{entranceId}' has no scene set.");
            return;
        }

        activeRun.checkpointScene      = scene;
        activeRun.checkpointEntranceId = entranceId;
        activeRun.hasCheckpoint        = true;

        SetResume(GameRunProfile.ResumeType.Checkpoint, scene, entranceId);
        MarkDirty();

        // The rest that opened the menu already healed.
        GoToCheckpoint(false);
    }

    public bool HasCheckpoint() {
        return activeRun.hasCheckpoint;
    }

    public string GetCheckpointEntranceId() {
        return activeRun.checkpointEntranceId;
    }

    // WALLS

    public void RegisterBrokenWall(string wallID) {
        activeRun.brokenWalls.Add(wallID);
        MarkDirty();
    }

    public bool IsWallBroken(string wallID) {
        return activeRun.brokenWalls.Contains(wallID);
    }

    // EVENTS - one-off world flags: shortcuts opened, cutscenes watched.

    public void RegisterEvent(string eventId) {
        if (string.IsNullOrEmpty(eventId)) return;

        activeRun.seenEvents.Add(eventId);
        MarkDirty();
    }

    public bool HasSeenEvent(string eventId) {
        return !string.IsNullOrEmpty(eventId) && activeRun.seenEvents.Contains(eventId);
    }

    // Every grant goes through here, so the card is never a thing a call site can forget.
    public void GrantAbility(AbilityType ability) {
        if (activeRun.IsAbilityUnlocked(ability)) return;

        activeRun.SetAbilityUnlocked(ability, true);
        MarkDirty();

        CoreAbilityInfo info = assetDatabase != null ? assetDatabase.FindCoreAbility(ability) : null;

        if (info == null) {
            Debug.LogWarning($"[GameManager] No CoreAbilityInfo for {ability} in the asset " +
                             "database, so it was granted without a card.", this);
            return;
        }

        AbilityGetUI.Show(info.icon, info.DisplayName, info.description);
    }

    public void GrantAbility(ActiveAbility ability) {
        if (ability == null || activeRun.OwnsAbility(ability)) return;

        activeRun.AddAbility(ability);
        MarkDirty();

        AbilityGetUI.Show(ability.icon, ability.abilityName, ability.description);
    }

    public System.Action<int> OnLumensChanged;

    public void AddLumens(int amount) {
        activeRun.lumens += amount;
        OnLumensChanged?.Invoke(activeRun.lumens);
        MarkDirty();
    }

    public void TakeLumens(int amount) {
        activeRun.lumens -= amount;
        OnLumensChanged?.Invoke(activeRun.lumens);
        MarkDirty();
    }

    public bool UseBundle(LumenBundle bundle) {
        if (bundle == null || !activeRun.ConsumeBundle(bundle)) return false;

        AddLumens(bundle.value);
        return true;
    }

    // PLAYER DEATH
    public void PlayerDied(float respawnDelay) {
        activeRun.temporaryRemoved.Clear();

        DropShade();

        CountDeath();
        SaveNow();

        StartCoroutine(RespawnRoutine(respawnDelay));
    }

    // Last safe ground, so a spike death does not strand the pile.
    private void DropShade() {
        // Dying replaces the pile even when broke.
        activeRun.droppedLumens = 0;
        activeRun.dropScene     = null;

        if (activeRun.lumens <= 0) return;

        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();

        // No player to read a spot from, so keep the lumens.
        if (player == null) return;

        activeRun.droppedLumens = activeRun.lumens;
        activeRun.dropScene     = activeRun.currentArea;
        activeRun.dropPosition  = player.LastSafeGround;

        activeRun.lumens = 0;
        OnLumensChanged?.Invoke(0);
    }

    // Clears the record only; the pickups carry the lumens.
    public void CollectShade() {
        if (!activeRun.HasShade) return;

        activeRun.droppedLumens = 0;
        activeRun.dropScene     = null;
    }

    private IEnumerator RespawnRoutine(float respawnDelay) {
        yield return new WaitForSecondsRealtime(respawnDelay);

        if (activeRun.hasCheckpoint) {
            GoToCheckpoint();
        }
        else {
            activeRun.currentHp = activeRun.maxHp;
            StartCoroutine(LoadSceneRoutine(startingScene));
        }
    }
}