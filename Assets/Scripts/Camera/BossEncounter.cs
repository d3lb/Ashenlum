using UnityEngine;

// Sequences the fight: doors shut, camera moves, boss wakes - then the reverse plus the
// reward when he dies. These are not separate features, they are two moments, so one
// thing orders them. Whether the boss still exists at all is the boss's own business.
public class BossEncounter : MonoBehaviour
{
    [Header("Boss")]
    [SerializeField] private SecretaryBirdBrain boss;
    [SerializeField] private SecretaryBirdHealth bossHealth;

    [Header("Doors")]
    [SerializeField] private GameObject[] doors;

    [Header("Camera")]
    [SerializeField] private CameraSwitcher cameraSwitcher;

    [Header("Reward")]
    [SerializeField] private AbilityType reward = AbilityType.Dash;

    // Cached, because the boss removes itself when it has already been beaten and the
    // trigger still needs to know that afterwards.
    private string bossId;

    private GameRunProfile Run => GameManager.Instance != null ? GameManager.Instance.activeRun : null;

    public bool AlreadyBeaten =>
        Run != null && !string.IsNullOrEmpty(bossId) && Run.defeatedBosses.Contains(bossId);

    private void Awake()
    {
        if (boss != null) bossId = boss.BossId;
        if (bossHealth != null) bossHealth.OnDied += OnBossDied;
    }

    private void OnDestroy()
    {
        if (bossHealth != null) bossHealth.OnDied -= OnBossDied;
    }

    private void Start() => SetDoorsClosed(false);

    public void Begin()
    {
        if (AlreadyBeaten || boss == null) return;

        SetDoorsClosed(true);

        if (cameraSwitcher != null) cameraSwitcher.SwitchToBossCam();

        boss.Activate();
    }

    private void OnBossDied()
    {
        Run?.defeatedBosses.Add(bossId);
        Run?.SetAbilityUnlocked(reward, true);

        SetDoorsClosed(false);

        if (cameraSwitcher != null) cameraSwitcher.SwitchToGameplayCam();
    }

    private void SetDoorsClosed(bool closed)
    {
        if (doors == null) return;

        foreach (GameObject door in doors)
            if (door != null) door.SetActive(closed);
    }
}
