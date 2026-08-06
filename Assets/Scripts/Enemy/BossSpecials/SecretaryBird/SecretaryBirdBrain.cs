using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the fight loop, every state transition, and every cleanup.
///
/// The old brain polled a state flag in Update and let each attack transition itself out.
/// That meant every attack was a place the exit contract could break - and one of them did,
/// freezing the fight permanently. Here the loop is a single coroutine with a fixed shape:
///
///     Idle -> Choose -> [attack, watchdogged] -> forced cleanup -> Recover -> Idle
///
/// An attack physically cannot strand the boss, because it is never asked to end anything.
/// </summary>
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
    [Tooltip("Recovery multiplier per phase. Lower = smaller punish window = harder.")]
    [SerializeField] private float[] recoveryScale = { 1f, 0.75f, 0.55f };

    [Header("Pacing")]
    [Tooltip("Breath between attacks. Keep small - this boss should feel relentless.")]
    [SerializeField] private float idleBeat = 0.2f;

    [Header("Debug")]
    [Tooltip("Prints every attack as it starts. For a live on-screen readout instead, "
             + "add SecretaryBirdDebugHUD to the boss.")]
    [SerializeField] private bool logAttacks;

    private readonly List<SecretaryBirdAttack> pool = new List<SecretaryBirdAttack>();
    private readonly List<SecretaryBirdAttack> bag  = new List<SecretaryBirdAttack>();
    private readonly List<int> candidates = new List<int>();

    private SecretaryBirdAttack last;
    private Coroutine loop;
    private Coroutine actRoutine;
    private bool actDone;
    private bool active;
    private SecretaryBirdAttack current;

    public bool Active => active;

    /// <summary>What is running right now. Read by SecretaryBirdDebugHUD.</summary>
    public SecretaryBirdAttack CurrentAttack => current;

    private void Awake()
    {
        if (state == null)     state     = GetComponent<SecretaryBirdState>();
        if (move == null)      move      = GetComponent<SecretaryBirdMovement>();
        if (hitboxes == null)  hitboxes  = GetComponent<SecretaryBirdAttackController>();
        if (telegraph == null) telegraph = GetComponentInChildren<SecretaryBirdTelegraph>(true);
        if (health == null)    health    = GetComponent<SecretaryBirdHealth>();

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
        state.CurrentState = SecretaryBirdState.BossStateType.Idle;
        loop = StartCoroutine(FightLoop());
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
            yield return new WaitForSeconds(idleBeat);

            if (player == null) { yield return null; continue; }

            UpdatePhase();

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
            yield return new WaitForSeconds(attack.Recovery * RecoveryScale());
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
        // `yield return inner` - NOT StartCoroutine(inner). This keeps every nested
        // movement and telegraph step inside THIS coroutine, so the StopCoroutine above
        // tears down the entire chain rather than orphaning half of it.
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
            bag.Clear(); // reshuffle so newly unlocked moves enter rotation immediately
        }
    }

    private float RecoveryScale()
    {
        if (recoveryScale == null || recoveryScale.Length == 0) return 1f;
        return recoveryScale[Mathf.Clamp(state.Phase - 1, 0, recoveryScale.Length - 1)];
    }

    /// <summary>
    /// Shuffle bag, not Random.Range. Guarantees the player sees the whole moveset before
    /// anything repeats, and makes frequency tuning trivial (weight 2 = two copies).
    ///
    /// On top of that: the same attack NEVER runs twice in a row. Nudging the rolled index
    /// was not enough - a weighted move has several copies in the bag, so the neighbour is
    /// often the same attack again. Every copy of the previous move is excluded up front.
    /// </summary>
    private SecretaryBirdAttack Draw()
    {
        float dist = Vector2.Distance(transform.position, player.position);

        for (int i = bag.Count - 1; i >= 0; i--)
            if (!bag[i].CanUse(dist, state.Phase)) bag.RemoveAt(i);

        if (bag.Count == 0) Refill(dist);
        if (bag.Count == 0) return null;

        candidates.Clear();
        for (int i = 0; i < bag.Count; i++)
            if (bag[i] != last) candidates.Add(i);

        // Bag is nothing but copies of the last move - refill and try once more before
        // accepting a repeat.
        if (candidates.Count == 0)
        {
            Refill(dist);
            for (int i = 0; i < bag.Count; i++)
                if (bag[i] != last) candidates.Add(i);
        }

        // Genuinely only one legal move right now (usually phase 1 with a range filter
        // active). A repeat is unavoidable - if you see this a lot, the phase needs
        // another attack in it.
        if (candidates.Count == 0)
            for (int i = 0; i < bag.Count; i++) candidates.Add(i);

        int pick = candidates[Random.Range(0, candidates.Count)];
        SecretaryBirdAttack chosen = bag[pick];
        bag.RemoveAt(pick);
        last = chosen;
        return chosen;
    }

    private void Refill(float dist)
    {
        bag.Clear();
        foreach (SecretaryBirdAttack a in pool)
        {
            if (!a.CanUse(dist, state.Phase)) continue;
            for (int i = 0; i < a.Weight; i++) bag.Add(a);
        }
    }
}
