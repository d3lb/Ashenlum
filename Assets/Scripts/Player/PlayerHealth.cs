using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Main Settings")]
    [SerializeField] private int hp = 100;
    [SerializeField] private int maxHp = 100;
    [SerializeField] private float iFrameTime = 0.3f;

    [Header("Regen Settings")]
    [SerializeField] private float regenDelay = 5f;
    [SerializeField] private float regenRate = 3f;
    private float regenBuffer;


    private float lastHitTime;
    private float iFrameTimer;
    private bool isInvincible;

    public int CurrentHP => hp;
    public int MaxHP => maxHp;
    public float LastHitTime => lastHitTime;

    
    public void Update()
    {
        if (isInvincible)
        {
            iFrameTimer -= Time.deltaTime;
            if (iFrameTimer <= 0)
                isInvincible = false;
        }

        if (Time.time >= lastHitTime + regenDelay)
        {
            Regenerate();
        }
    }

    public void TakeDamage(int dmg, Vector2 attackerPos)
    {
        if (isInvincible)
            return;

        lastHitTime = Time.time;
        regenBuffer = 0f;

        hp -= dmg;

        isInvincible = true;
        iFrameTimer = iFrameTime;

        if (hp <= 0)
            Die();
    }

    private void Regenerate()
    {
        if (hp >= maxHp)
            return;

        regenBuffer += regenRate * Time.deltaTime;

        if (regenBuffer >= 1f)
        {
            int amount = Mathf.FloorToInt(regenBuffer);

            Heal(amount);   
            regenBuffer -= amount;

        }
    }

    public void Heal(int amount)
    {
        hp += amount;

        if (hp > maxHp)
            hp = maxHp;
    }


    void Die()
    {
        Debug.Log("Player dead");
    }
}