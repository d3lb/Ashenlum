using System.Collections;
using UnityEngine;

// Doors, camera and King wake as one moment, so one thing has to order them.
// Same shape as BossEncounter, kept separate so the bird cannot break when this changes.
public class KingEncounter : MonoBehaviour
{
    [Header("King")]
    [SerializeField] private KingBrain king;
    [SerializeField] private KingHealth kingHealth;

    [Header("Doors")]
    [SerializeField] private GameObject[] doors;

    [Header("Camera")]
    [SerializeField] private CameraSwitcher cameraSwitcher;

    [Header("Ending")]
    [SerializeField] private KingCredits credits;
    [SerializeField] private Conversation lastWords;

    // Cached: the King deletes himself once beaten, and the throne still asks after that.
    private string bossId;

    private GameRunProfile Run => GameManager.Instance != null ? GameManager.Instance.activeRun : null;

    public bool AlreadyBeaten =>
        Run != null && !string.IsNullOrEmpty(bossId) && Run.defeatedBosses.Contains(bossId);

    public bool Started { get; private set; }

    private void Awake()
    {
        if (king != null) bossId = king.BossId;
        if (kingHealth != null) kingHealth.OnDied += OnKingDied;
    }

    private void OnDestroy()
    {
        if (kingHealth != null) kingHealth.OnDied -= OnKingDied;
    }

    private void Start() => SetDoorsClosed(false);

    public void Begin()
    {
        if (Started || AlreadyBeaten || king == null) return;

        Started = true;

        SetDoorsClosed(true);

        if (cameraSwitcher != null) cameraSwitcher.SwitchToBossCam();

        king.Activate();
    }

    private void OnKingDied()
    {
        Run?.defeatedBosses.Add(bossId);

        // Beating the final boss is the least acceptable thing to lose to a crash.
        if (GameManager.Instance != null) GameManager.Instance.SaveNow();

        StartCoroutine(Ending());
    }

    private IEnumerator Ending()
    {
        if (lastWords != null && DialogueManager.Instance != null)
        {
            bool done = false;
            DialogueManager.Instance.StartDialogue(lastWords, () => done = true);
            yield return new WaitUntil(() => done);
        }

        if (credits != null) yield return credits.Play();

        // Doors and camera only after the card, so the room is still sealed behind
        // the black and you come back to the arena rather than mid-transition.
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
