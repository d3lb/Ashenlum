using UnityEngine;
using static PlayerState;

public class PlayerAbility : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerState state;


    [Header("Burst Settings")]
    [SerializeField] private float burstRadius = 7f;
    [SerializeField] private int burstDamage = 5;
    [SerializeField] private int burstHeal = 15;
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
    }


    private void Update()
    {
        if (Input.GetKey(KeyCode.F))
        {
            StartOrContinueCharge();
        }
        else
        {
            CancelCharge();
        }
    }

    private void TryBurst()
    {

        if (Time.time < playerHealth.LastHitTime + postHitLockTime)
            return;


        if (Time.time < lastBurstTime + burstCooldown)
            return;
        
        DoBurst();
        lastBurstTime = Time.time;
    }

    private void DoBurst()
    {
        // heal player
        playerHealth.Heal(burstHeal);

        // damage enemies in radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            burstRadius,
            enemyLayer
        );

        foreach (var hit in hits)
        {
            hit.GetComponent<EnemyHealth>()?.TakeDamage(burstDamage, transform.position);
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
            lastBurstTime = Time.time;
            CancelCharge(); // reset state
        }
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