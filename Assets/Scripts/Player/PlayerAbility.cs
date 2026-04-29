using UnityEngine;

public class PlayerAbility : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Burst Settings")]
    [SerializeField] private float burstRadius = 7f;
    [SerializeField] private int burstDamage = 5;
    [SerializeField] private int burstHeal = 15;
    [SerializeField] private float burstCooldown = 10f;
    [SerializeField] private float postHitLockTime = 0.5f;
    [SerializeField] private LayerMask enemyLayer;

    private float lastBurstTime;
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryBurst();
        }
    }

    private void TryBurst()
    {
        Debug.Log("Check");
        // cooldown check

        if (Time.time < playerHealth.LastHitTime + postHitLockTime)
            return;
        Debug.Log("Passed HitLockTime");


        if (Time.time < lastBurstTime + burstCooldown)
            return;
        Debug.Log("Passed Cooldown");

        
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

    // debug gizmo
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, burstRadius);
    }
}