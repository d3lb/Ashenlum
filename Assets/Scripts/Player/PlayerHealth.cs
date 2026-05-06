using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Main Settings")]
    [SerializeField] private int hp = 100;
    [SerializeField] private int maxHp = 100;
    [SerializeField] private float iFrameTime = 0.3f;
    [SerializeField] private float damagedPauseTime = 0.05f;

    [Header("Regen Settings")]
    [SerializeField] private bool regenerate = false;
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

        if (Time.time >= lastHitTime + regenDelay && regenerate)
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
        TimeManager.Instance.HitStop(damagedPauseTime);

        if (hp <= 0)
            Die();
    }

    private void Regenerate()
    {
        if (hp >= maxHp) return;

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