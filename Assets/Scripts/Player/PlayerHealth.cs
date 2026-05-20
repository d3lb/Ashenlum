using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Main Settings")]
    [SerializeField] private int hp = 100;
    [SerializeField] private int maxHp = 100;
    [SerializeField] private float iFrameTime = 0.3f;

    [Header("Getting Hit")]
    [SerializeField] private float hitRecoilX = 8f;
    [SerializeField] private float hitRecoilY = 4f;
    [SerializeField] private float damagedPauseTime = 0.05f;
    [SerializeField] private float hitShakeDuration = 0.1f;
    [SerializeField] private float hitShakeAmplitude = 3f;
    [SerializeField] private float hitShakeFrequency = 3f;
    [SerializeField] private PlayerState state;

    [Header("Regen Settings")]
    [SerializeField] private bool regenerate = false;
    [SerializeField] private float regenDelay = 5f;
    [SerializeField] private float regenRate = 3f;
    private float regenBuffer;

    private Rigidbody2D rb;
    private float lastHitTime;
    private float iFrameTimer;
    private bool isInvincible;

    public int CurrentHP => hp;
    public int MaxHP => maxHp;
    public float LastHitTime => lastHitTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

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
        CameraShake.Instance.Shake(hitShakeDuration, hitShakeAmplitude, hitShakeFrequency);
        ApplyHitRecoil(attackerPos);


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
        state.CurrentState = PlayerState.PlayerStateType.Dead;
        GetComponent<Collider2D>().enabled = false;
        rb.linearVelocity = Vector2.zero;
        this.enabled = false;
    }


    // Apply hit recoil to player when hit
    private void ApplyHitRecoil(Vector2 attackerPos)
    {
        Vector2 dir = (transform.position - (Vector3)attackerPos).normalized;

        rb.linearVelocity = Vector2.zero;

        rb.AddForce(
            new Vector2(dir.x * hitRecoilX, hitRecoilY),
            ForceMode2D.Impulse
        );
    }
}