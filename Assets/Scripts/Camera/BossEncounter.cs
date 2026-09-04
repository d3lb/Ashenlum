using UnityEngine;

// Doors, camera and boss wake as one moment, so one thing has to order them.
public class BossEncounter : MonoBehaviour {
    [Header("Boss")]
    [SerializeField] private SecretaryBirdBrain boss;
    [SerializeField] private SecretaryBirdHealth bossHealth;

    [Header("Doors")]
    [SerializeField] private GameObject[] doors;

    [Header("Camera")]
    [SerializeField] private CameraSwitcher cameraSwitcher;

    [Header("Reward")]
    [SerializeField] private AbilityType reward = AbilityType.Dash;

    // Cached: the boss deletes itself once beaten, and the trigger still asks after that.
    private string bossId;

    private GameRunProfile Run => GameManager.Instance != null ? GameManager.Instance.activeRun : null;

    public bool AlreadyBeaten =>
        Run != null && !string.IsNullOrEmpty(bossId) && Run.defeatedBosses.Contains(bossId);

    private void Awake() {
        if (boss != null) bossId = boss.BossId;
        if (bossHealth != null) bossHealth.OnDied += OnBossDied;
    }

    private void OnDestroy() {
        if (bossHealth != null) bossHealth.OnDied -= OnBossDied;
    }

    private void Start() => SetDoorsClosed(false);

    public void Begin() {
        if (AlreadyBeaten || boss == null) return;

        SetDoorsClosed(true);

        if (cameraSwitcher != null) cameraSwitcher.SwitchToBossCam();

        boss.Activate();
    }

    private void OnBossDied() {
        Run?.defeatedBosses.Add(bossId);
        Run?.SetAbilityUnlocked(reward, true);

        // A boss kill is the least acceptable thing to lose to a crash.
        if (GameManager.Instance != null) GameManager.Instance.SaveNow();

        SetDoorsClosed(false);

        if (cameraSwitcher != null) cameraSwitcher.SwitchToGameplayCam();
    }

    private void SetDoorsClosed(bool closed) {
        if (doors == null) return;

        foreach (GameObject door in doors)
            if (door != null) door.SetActive(closed);
    }
}
