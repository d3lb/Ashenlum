using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(KingState))]
public class KingBrain : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private KingState state;
    [SerializeField] private KingHealth health;
    [SerializeField] private KingPacing pacing;
    [SerializeField] private Transform player;

    [Header("Phases (fraction of max health)")]
    [SerializeField] private float phase2At = 0.66f;
    [SerializeField] private float phase3At = 0.33f;

    // Fired once when a phase boundary is crossed. The "get away from me" burst.
    [Header("Scripted attacks")]
    [SerializeField] private KingAttack transitionAttack;

    // Fired when the player attacks him too fast. Punishes standing on top of him.
    [SerializeField] private KingAttack punishAttack;
    [SerializeField] private int punishHitCount = 4;
    [SerializeField] private float punishWindow = 2f;

    // Without this he answers every burst, so staying on him turns the fight into the
    // punish on repeat. It is a warning, not a wall.
    [SerializeField] private float punishCooldown = 8f;

    // The whole point of the fight: the worse your stability, the less time he gives
    // you. Low is where you hit hardest, so it is also where he is most relentless.
    [Header("Greed")]
    [SerializeField] private float highStabilityScale = 1.4f;
    [SerializeField] private float midStabilityScale = 1f;
    [SerializeField] private float lowStabilityScale = 0.6f;

    // Shared by every attack, so damage and the player layer are set in one place
    // rather than repeated on each one where they would drift apart.
    [Header("Light")]
    [SerializeField] private int lightDamage = 15;
    [SerializeField] private LayerMask playerLayer;

    // Left empty a plain white square is generated in code, so the fight is visible
    // and tunable before any art exists. Drop a sprite in later and nothing changes.
    [SerializeField] private Sprite lightSprite;

    [SerializeField] private Color telegraphColor = new Color(1f, 0.93f, 0.55f, 0.30f);
    [SerializeField] private Color activeColor = new Color(1f, 1f, 0.85f, 0.95f);

    // Must beat the tilemap's order or every attack draws behind the floor.
    [SerializeField] private string sortingLayer = "Default";
    [SerializeField] private int sortingOrder = 50;

    [Header("Intro")]
    [SerializeField] private string bossId = "KingOfLum";
    [SerializeField] private Conversation introConversation;
    [SerializeField] private Conversation phase3Conversation;
    [SerializeField] private float introDelay = 1.5f;

    [Header("Debug")]
    [SerializeField] private bool logAttacks;

    private readonly List<KingAttack> pool = new();
    private readonly List<KingAttack> bag = new();
    private readonly List<int> candidates = new();
    private readonly List<Coroutine> running = new();

    private KingAttack last;
    private Coroutine loop;
    private bool active;
    private bool dialogueDone;

    private PlayerHealth playerHealth;
    private int recentHits;
    private float recentHitsExpire;
    private bool punishQueued;
    private float punishReadyAt;

    public string BossId => bossId;
    public bool Active => active;
    public Transform Player => player;

    // Read by KingDebugHUD. Nothing in the fight depends on these.
    public KingAttack CurrentMain { get; private set; }
    public KingAttack CurrentExtra { get; private set; }
    public KingAttack TransitionAttack => transitionAttack;
    public KingAttack PunishAttack => punishAttack;
    public IReadOnlyList<KingAttack> Pool => pool;
    public int PunishHitsNeeded => punishHitCount;
    public int PunishProgress => Time.time > recentHitsExpire ? 0 : recentHits;
    public float GreedNow => GreedScale;
    public KingPhaseTuning PaceNow => Pace;

    private KingPhaseTuning Pace => pacing != null ? pacing.For(state.Phase) : fallbackPace;
    private static readonly KingPhaseTuning fallbackPace = new();

    public float TelegraphScale => Pace.telegraphScale;

    public int LightDamage => lightDamage;
    public LayerMask PlayerLayer => playerLayer;

    public Sprite LightSprite => lightSprite;
    public Color TelegraphColor => telegraphColor;
    public Color ActiveColor => activeColor;
    public string SortingLayer => sortingLayer;
    public int SortingOrder => sortingOrder;

    // 1 when there is no player to read, so a missing reference cannot break pacing.
    private float GreedScale
    {
        get
        {
            if (playerHealth == null) return 1f;

            PlayerHealth.StabilityState tier = playerHealth.CurrentStabilityState;

            if (tier == PlayerHealth.StabilityState.High) return highStabilityScale;
            if (tier == PlayerHealth.StabilityState.Mid) return midStabilityScale;
            return lowStabilityScale;
        }
    }

    private void Awake()
    {
        if (state == null) state = GetComponent<KingState>();
        if (health == null) health = GetComponent<KingHealth>();
        if (pacing == null) pacing = GetComponent<KingPacing>();

        GetComponents(pool);

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player != null) playerHealth = player.GetComponent<PlayerHealth>();

        if (health != null) health.OnHit += RegisterHit;
    }

    private void OnDestroy()
    {
        if (health != null) health.OnHit -= RegisterHit;
    }

    private void Start()
    {
        // Same rule the bird and the breakables use: already dealt with, so not here.
        var run = GameManager.Instance != null ? GameManager.Instance.activeRun : null;
        if (run != null && run.defeatedBosses.Contains(bossId)) Destroy(gameObject);
    }

    // Called by the throne when the player challenges him.
    public void Activate()
    {
        if (active || state.IsDead) return;

        active = true;
        loop = StartCoroutine(Begin());
    }

    public void Deactivate()
    {
        active = false;

        StopRunning();
        if (loop != null) StopCoroutine(loop);
        loop = null;

        if (!state.IsDead) state.CurrentState = KingState.KingStateType.Idle;
    }

    private IEnumerator Begin()
    {
        state.CurrentState = KingState.KingStateType.Intro;

        // FirstTime last: it marks the line as seen, so calling it before the null
        // checks would burn the intro on a run where no conversation was assigned yet.
        if (introConversation != null && DialogueManager.Instance != null && FirstTime("intro"))
            yield return Say(introConversation);
        else if (introDelay > 0f)
            yield return new WaitForSeconds(introDelay);

        // Nested, not StartCoroutine, so Deactivate's StopCoroutine(loop) still reaches it.
        yield return FightLoop();
    }

    // Saved in seenEvents, so a line never plays twice even across deaths and reloads.
    // Marks it as seen on the way out, so asking is also answering.
    private bool FirstTime(string key)
    {
        var run = GameManager.Instance != null ? GameManager.Instance.activeRun : null;
        if (run == null) return true;

        string id = $"{bossId}_{key}";
        if (run.seenEvents.Contains(id)) return false;

        run.seenEvents.Add(id);
        GameManager.Instance.MarkDirty();
        return true;
    }

    private IEnumerator Say(Conversation conversation)
    {
        dialogueDone = false;
        DialogueManager.Instance.StartDialogue(conversation, () => dialogueDone = true);
        yield return new WaitUntil(() => dialogueDone);
    }

    private IEnumerator FightLoop()
    {
        while (active && !state.IsDead)
        {
            state.CurrentState = KingState.KingStateType.Idle;

            if (UpdatePhase())
                yield return RunTransition();

            // Greed applies to the gap as well as the recovery, or he would only feel
            // faster after an attack rather than throughout.
            yield return new WaitForSeconds(Pace.idleBeat * GreedScale);

            if (player == null) { yield return null; continue; }

            if (punishQueued && punishAttack != null && Time.time >= punishReadyAt)
            {
                punishQueued = false;
                punishReadyAt = Time.time + punishCooldown;

                yield return RunAttacks(punishAttack, null);
                continue;
            }

            // Queued but still cooling down: drop it rather than saving it up, or he
            // fires a stale punish long after the greed that earned it.
            punishQueued = false;

            state.CurrentState = KingState.KingStateType.Choosing;

            KingAttack main = Draw();
            if (main == null) { yield return null; continue; }

            // Phase 2 and 3 press by stacking a second move on the same beat, which
            // gives them an identity beyond simply shorter gaps.
            KingAttack extra = null;
            if (main.CanOverlap && Random.value < Pace.doubleUpChance)
                extra = Draw();

            if (logAttacks)
                Debug.Log($"[King] phase {state.Phase} -> {main.DisplayName}" +
                          (extra != null ? $" + {extra.DisplayName}" : ""), this);

            CurrentMain = main;
            CurrentExtra = extra;

            yield return RunAttacks(main, extra);

            CurrentMain = null;
            CurrentExtra = null;

            state.CurrentState = KingState.KingStateType.Recover;
            yield return new WaitForSeconds(main.Recovery * Pace.recoveryScale * GreedScale);
        }
    }

    private IEnumerator RunTransition()
    {
        state.CurrentState = KingState.KingStateType.Transition;

        if (state.Phase == 3 && phase3Conversation != null &&
            DialogueManager.Instance != null && FirstTime("phase3"))
            yield return Say(phase3Conversation);

        if (transitionAttack != null)
            yield return RunAttacks(transitionAttack, null);
    }

    // Waits for every attack started here, so an overlapping pair cannot leak into
    // the next beat and stack forever.
    private IEnumerator RunAttacks(KingAttack a, KingAttack b)
    {
        StopRunning();

        state.CurrentState = KingState.KingStateType.Attacking;

        float deadline = Time.time + Mathf.Max(a != null ? a.Timeout : 0f,
                                               b != null ? b.Timeout : 0f);

        int finished = 0;
        int expected = (a != null ? 1 : 0) + (b != null ? 1 : 0);

        if (a != null) running.Add(StartCoroutine(Wrap(a, () => finished++)));
        if (b != null) running.Add(StartCoroutine(Wrap(b, () => finished++)));

        while (finished < expected && Time.time < deadline && !state.IsDead)
            yield return null;

        if (finished < expected)
            Debug.LogWarning($"[King] an attack timed out and was aborted.", this);

        StopRunning();
    }

    private IEnumerator Wrap(KingAttack attack, System.Action onDone)
    {
        yield return attack.Act(player);
        onDone();
    }

    private void StopRunning()
    {
        foreach (Coroutine c in running)
            if (c != null) StopCoroutine(c);

        running.Clear();
    }

    // Counts hits inside a rolling window. Hitting him steadily is fine; burst damage
    // at point blank is what gets answered.
    private void RegisterHit()
    {
        if (Time.time > recentHitsExpire) recentHits = 0;

        recentHits++;
        recentHitsExpire = Time.time + punishWindow;

        if (recentHits < punishHitCount) return;

        recentHits = 0;
        punishQueued = true;
    }

    private bool UpdatePhase()
    {
        float f = health != null ? health.Normalized : 1f;
        int target = f <= phase3At ? 3 : f <= phase2At ? 2 : 1;

        // Never backwards, and never more than one step at a time. A single big hit can
        // cross both thresholds, and jumping straight to 3 would skip phase 2's
        // transition burst and its dialogue entirely.
        if (target <= state.Phase) return false;

        state.Phase++;
        bag.Clear();
        return true;
    }

    private KingAttack Draw()
    {
        for (int i = bag.Count - 1; i >= 0; i--)
            if (!bag[i].CanUse(state.Phase)) bag.RemoveAt(i);

        if (bag.Count == 0) Refill();
        if (bag.Count == 0) return null;

        candidates.Clear();
        for (int i = 0; i < bag.Count; i++)
            if (bag[i] != last) candidates.Add(i);

        if (candidates.Count == 0)
            for (int i = 0; i < bag.Count; i++) candidates.Add(i);

        int pick = candidates[Random.Range(0, candidates.Count)];
        KingAttack chosen = bag[pick];

        bag.RemoveAt(pick);
        last = chosen;
        return chosen;
    }

    private void Refill()
    {
        bag.Clear();

        foreach (KingAttack a in pool)
        {
            if (!a.CanUse(state.Phase)) continue;
            for (int i = 0; i < a.Weight; i++) bag.Add(a);
        }
    }
}
