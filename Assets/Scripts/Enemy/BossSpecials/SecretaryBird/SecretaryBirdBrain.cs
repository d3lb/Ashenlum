using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SecretaryBirdState))]
public class SecretaryBirdBrain : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SecretaryBirdState state;
    [SerializeField] private SecretaryBirdMovement move;
    [SerializeField] private SecretaryBirdAttackController hitboxes;
    [SerializeField] private SecretaryBirdTelegraph telegraph;
    [SerializeField] private SecretaryBirdHealth health;
    [SerializeField] private Transform player;

    [Header("Phases (fraction of max health)")]
    [SerializeField] private float phase2At = 0.66f;
    [SerializeField] private float phase3At = 0.33f;

    [Header("Pacing")]
    [SerializeField] private SecretaryBirdPacing pacing;

    [Header("Intro")]
    [SerializeField] private float introDelay = 2f;
    [SerializeField] private bool waitForCue;
    [SerializeField] private UnityEngine.Events.UnityEvent onIntro;

    [Header("Debug")]
    [SerializeField] private bool logAttacks;

    private readonly List<SecretaryBirdAttack> pool = new List<SecretaryBirdAttack>();
    private readonly List<SecretaryBirdAttack> bag  = new List<SecretaryBirdAttack>();
    private readonly List<int> candidates = new List<int>();

    private SecretaryBirdAttack last;
    private Coroutine loop;
    private Coroutine actRoutine;
    private bool actDone;
    private bool active;
    private bool introCued;
    private SecretaryBirdAttack current;

    public bool Active => active;

    public SecretaryBirdAttack CurrentAttack => current;

    private void Awake()
    {
        if (state == null)     state     = GetComponent<SecretaryBirdState>();
        if (move == null)      move      = GetComponent<SecretaryBirdMovement>();
        if (hitboxes == null)  hitboxes  = GetComponent<SecretaryBirdAttackController>();
        if (telegraph == null) telegraph = GetComponentInChildren<SecretaryBirdTelegraph>(true);
        if (health == null)    health    = GetComponent<SecretaryBirdHealth>();
        if (pacing == null)    pacing    = GetComponent<SecretaryBirdPacing>();

        GetComponents(pool);

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    public void Activate()
    {
        if (active || state.IsDead) return;
        active = true;
        loop = StartCoroutine(Begin());
    }

    public void FinishIntro() => introCued = true;

    private IEnumerator Begin()
    {
        state.CurrentState = SecretaryBirdState.BossStateType.Intro;
        CleanUp();

        introCued = false;
        onIntro?.Invoke();

        if (waitForCue)
            yield return new WaitUntil(() => introCued);
        else if (introDelay > 0f)
            yield return new WaitForSeconds(introDelay);

        // Nested, not StartCoroutine - Deactivate's StopCoroutine(loop) must still reach it.
        yield return FightLoop();
    }

    public void Deactivate()
    {
        active = false;

        if (actRoutine != null) StopCoroutine(actRoutine);
        if (loop != null)       StopCoroutine(loop);
        actRoutine = null;
        loop = null;

        CleanUp();
        if (!state.IsDead) state.CurrentState = SecretaryBirdState.BossStateType.Idle;
    }

    private IEnumerator FightLoop()
    {
        while (active && !state.IsDead)
        {
            state.CurrentState = SecretaryBirdState.BossStateType.Idle;

            // Phase before the beat, so a phase change shortens the very next gap.
            UpdatePhase();
            yield return new WaitForSeconds(Pace.idleBeat);

            if (player == null) { yield return null; continue; }

            state.CurrentState = SecretaryBirdState.BossStateType.Choosing;
            SecretaryBirdAttack attack = Draw();
            if (attack == null) { yield return null; continue; }

            current = attack;
            if (logAttacks)
                Debug.Log($"[SecretaryBird] phase {state.Phase}  ->  {attack.DisplayName}", this);

            yield return RunAttack(attack);

            // The punish window is owned HERE, so it can never be skipped by a bad attack.
            current = null;
            state.CurrentState = SecretaryBirdState.BossStateType.Recover;
            move.Stop();
            move.ResetGravity();
            yield return new WaitForSeconds(attack.Recovery * Pace.recoveryScale);
        }
    }

    private IEnumerator RunAttack(SecretaryBirdAttack attack)
    {
        actDone = false;
        actRoutine = StartCoroutine(Wrap(attack.Act(player)));

        float deadline = Time.time + attack.Timeout;
        while (!actDone && Time.time < deadline && !state.IsDead)
            yield return null;

        if (!actDone && actRoutine != null)
        {
            // Watchdog. No attack can hang the fight, whatever it got stuck on.
            StopCoroutine(actRoutine);
            Debug.LogWarning($"[SecretaryBird] '{attack.DisplayName}' timed out after " +
                             $"{attack.Timeout}s and was aborted.");
        }

        actRoutine = null;
        CleanUp();
    }

    private IEnumerator Wrap(IEnumerator inner)
    {
        yield return inner;
        actDone = true;
    }

    private void CleanUp()
    {
        if (hitboxes != null)  hitboxes.DisableAllHitboxes();
        if (telegraph != null) telegraph.Clear();
        if (move != null)
        {
            move.Stop();
            move.ResetGravity();
            move.ClampInsideArena();
        }
    }

    private void UpdatePhase()
    {
        float f = health != null ? health.Normalized : 1f;
        int p = f <= phase3At ? 3 : f <= phase2At ? 2 : 1;

        if (p != state.Phase)
        {
            state.Phase = p;
            bag.Clear();
        }
    }

    private static readonly PhaseTuning fallbackPace = new PhaseTuning();

    private PhaseTuning Pace => pacing != null ? pacing.For(state.Phase) : fallbackPace;

    private SecretaryBirdAttack Draw()
    {
        for (int i = bag.Count - 1; i >= 0; i--)
            if (!bag[i].CanUse(state.Phase)) bag.RemoveAt(i);

        if (bag.Count == 0) Refill();
        if (bag.Count == 0) return null;

        candidates.Clear();
        for (int i = 0; i < bag.Count; i++)
            if (bag[i] != last) candidates.Add(i);

        if (candidates.Count == 0)
        {
            Refill();
            for (int i = 0; i < bag.Count; i++)
                if (bag[i] != last) candidates.Add(i);
        }

        if (candidates.Count == 0)
            for (int i = 0; i < bag.Count; i++) candidates.Add(i);

        int pick = candidates[Random.Range(0, candidates.Count)];
        SecretaryBirdAttack chosen = bag[pick];
        bag.RemoveAt(pick);
        last = chosen;
        return chosen;
    }

    private void Refill()
    {
        bag.Clear();
        foreach (SecretaryBirdAttack a in pool)
        {
            if (!a.CanUse(state.Phase)) continue;
            for (int i = 0; i < a.Weight; i++) bag.Add(a);
        }
    }
}
