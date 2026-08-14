using UnityEngine;
using static PlayerState;
using System.Collections;

public class PlayerAbility : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerState state;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject wingBurstPrefab;
    [SerializeField] private EffectSpawner effectSpawner;
    private PlayerInput input;

    [Header("Burst Settings")]
    [SerializeField] private float burstRadius = 7f;
    [SerializeField] private int minBurstDamage = 1;
    [SerializeField] private int maxBurstDamage = 10; 
    // Fraction of max health, so retuning maxHp never silently rebalances the heal.
    [SerializeField, Range(0f, 1f)] private float burstHealPercent = 0.2f;
    [SerializeField] private float burstCooldown = 10f;
    [SerializeField] private float postHitLockTime = 0.5f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float burstChargeTime = 1f;
    [SerializeField] private float burstSlowTime = 0.5f;
    private Rigidbody2D rb;

    private Vector2 burstStartVelocity;
    private float burstTimer;
    private bool isCharging;
    private float lastBurstTime;
    public float CooldownPercent
    {
        get
        {
            float timePassed = Time.time - lastBurstTime;
            return Mathf.Clamp01(timePassed / burstCooldown);
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<PlayerInput>();
        lastBurstTime = -burstCooldown;
    }


    private void Update()
    {
        if (input.HealAbilityHeld)
        {
            StartOrContinueCharge();
        }
        else
        {
            CancelCharge();
        }
    }

    private void DoBurst()
    {
        // heal player
        float percent = burstHealPercent + (GameManager.Instance != null
            ? GameManager.Instance.activeRun.bonusHealPercent : 0f);
        playerHealth.Heal(Mathf.RoundToInt(playerHealth.MaxHP * percent));

        float missingPercent = 1f - ((float)playerHealth.CurrentHP / playerHealth.MaxHP);
        int damage = Mathf.RoundToInt(Mathf.Lerp(minBurstDamage, maxBurstDamage, missingPercent));

        // damage enemies in radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            burstRadius,
            enemyLayer
        );

        foreach (var hit in hits)
        {
            hit.GetComponent<EnemyHealth>()?.TakeDamage(damage, transform.position);
            effectSpawner.SpawnHitEffect(hit.transform.position, false);
        }
    }

    private void StartOrContinueCharge()
    {
        if (!isCharging)
        {
            if (Time.time < lastBurstTime + burstCooldown)
                return;

            if (Time.time < playerHealth.LastHitTime + postHitLockTime)
                return;

            isCharging = true;
            state.CurrentState = PlayerStateType.Burst;
            burstStartVelocity = rb.linearVelocity;
            burstTimer = 0f;
            state.IsUsingAbility = true;
        }

        // cancel if hit
        if (Time.time <= playerHealth.LastHitTime + 0.01f)
        {
            CancelCharge();
            return;
        }



        // charge time
        burstTimer += Time.deltaTime;

        float t = burstTimer / burstSlowTime;
        t = Mathf.Clamp01(t);
        rb.linearVelocity = Vector2.Lerp(burstStartVelocity * 0.5f, Vector2.zero, t);

        if (burstTimer >= burstChargeTime)
        {
            DoBurst();
            animator.SetTrigger("BurstRelease");
            Instantiate(wingBurstPrefab, transform.position, Quaternion.identity);
            lastBurstTime = Time.time;
            StartCoroutine(FinishBurst());
        }
    }
    private IEnumerator FinishBurst()
    {
        isCharging = false;

        yield return new WaitForSeconds(0.2f);

        state.IsUsingAbility = false;

        burstTimer = 0f;
    }

    private void CancelCharge()
    {
        if (!isCharging)
            return;

        isCharging = false;
        state.IsUsingAbility = false;
        burstTimer = 0f;
    }

    // debug gizmo
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, burstRadius);
    }
}