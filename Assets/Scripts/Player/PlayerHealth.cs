using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using static PlayerState;

public class PlayerHealth : MonoBehaviour
{
    [Header("Main Settings")]
    [SerializeField] private int hp = 100;
    [SerializeField] private int baseMaxHp = 100;
    private int maxHp = 100;
    [SerializeField] private float iFrameTime = 0.3f;
    [SerializeField] private float respawnDelay = 1.5f;

    [Header("Getting Hit")]
    [SerializeField] private float hitRecoilX = 8f;
    [SerializeField] private float hitRecoilY = 4f;
    [SerializeField] private float damagedPauseTime = 0.05f;
    [SerializeField] private float hitShakeDuration = 0.1f;
    [SerializeField] private float hitShakeAmplitude = 3f;
    [SerializeField] private float hitShakeFrequency = 3f;

    [Header("Regen Settings")]
    [SerializeField] private bool regenerate = false;
    [SerializeField] private float regenDelay = 5f;
    [SerializeField] private float regenRate = 3f;

    private float regenBuffer;
    private PlayerState state;
    private Rigidbody2D rb;
    private float lastHitTime;
    private float iFrameTimer;
    private bool isInvincible;

    public int CurrentHP => hp;
    public int MaxHP => maxHp;
    public float LastHitTime => lastHitTime;

    public enum StabilityState
    {
        High,
        Mid,
        Low
    }

    public StabilityState CurrentStabilityState
    {
        get
        {
            float percent = (float)hp / maxHp;

            if (percent > 0.66f)
                return StabilityState.High;

            if (percent > 0.33f)
                return StabilityState.Mid;

            return StabilityState.Low;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        state = GetComponent<PlayerState>();
    }

    private void Start()
    {

        var run = GameManager.Instance?.activeRun;

        if (run != null)
        {
            maxHp = baseMaxHp + run.bonusMaxHp;
            run.maxHp = maxHp;
            if (run.currentHp < 0) run.currentHp = maxHp;
            hp = Mathf.Clamp(run.currentHp, 0, maxHp);
        }
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
        if (hp <= 0 || isInvincible) return;

        lastHitTime = Time.time;
        regenBuffer = 0f;
        hp -= dmg;
        isInvincible = true;
        iFrameTimer = iFrameTime;

        if (TimeManager.Instance != null) TimeManager.Instance.HitStop(damagedPauseTime);
        if (CameraShakeManager.Instance != null) CameraShakeManager.Instance.Shake(hitShakeDuration, hitShakeAmplitude, hitShakeFrequency);

        ApplyHitRecoil(attackerPos);

        if (hp <= 0) Die();
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

    // Called by the shop so a purchase is felt immediately, not next scene.
    public void RefreshMaxHealth()
    {
        var run = GameManager.Instance?.activeRun;
        if (run == null) return;

        int gained = (baseMaxHp + run.bonusMaxHp) - maxHp;
        maxHp = baseMaxHp + run.bonusMaxHp;
        run.maxHp = maxHp;
        if (gained > 0) Heal(gained);
    }

    public void Heal(int amount)
    {
        hp += amount;
        if (hp > maxHp) hp = maxHp;
    }

    private void Die()
    {
        if (state != null && state.CurrentState == PlayerStateType.Dead) return;
        if (state != null) state.CurrentState = PlayerStateType.Dead;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        GameManager.Instance.PlayerDied(respawnDelay);
    }

    private void ApplyHitRecoil(Vector2 attackerPos)
    {
        Vector2 dir = (transform.position - (Vector3)attackerPos).normalized;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(new Vector2(dir.x * hitRecoilX, hitRecoilY), ForceMode2D.Impulse);
        }
    }
}