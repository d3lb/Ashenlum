using System.Collections;
using UnityEngine;

public class KingHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHp = 90;

    [Header("Flash")]
    [SerializeField] private float hitFlashTime = 0.15f;

    [Header("Invincibility")]
    [SerializeField] private float iFrameTime = 0.1f;

    [Header("Death")]
    [SerializeField] private float deathDelay = 1.2f;
    [SerializeField] private GameObject deathEffect;

    [Header("References")]
    [SerializeField] private KingState state;
    [SerializeField] private KingBrain brain;
    [SerializeField] private SpriteRenderer sprite;

    private int hp;
    private float iFrameTimer;
    private bool isInvincible;
    private Material mat;
    private Coroutine flashCoroutine;

    public int CurrentHP => hp;
    public int MaxHP => maxHp;
    public float Normalized => maxHp <= 0 ? 0f : Mathf.Clamp01((float)hp / maxHp);

    public System.Action<float> OnHealthChanged;
    public System.Action OnDied;

    // Fires on every hit that lands, so the brain can count aggression for Retribution.
    public System.Action OnHit;

    private void Awake()
    {
        hp = maxHp;

        if (state == null)  state  = GetComponent<KingState>();
        if (brain == null)  brain  = GetComponent<KingBrain>();
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();

        if (sprite != null)
            mat = sprite.material = new Material(sprite.material);
    }

    // Lets maxHp be edited during Play and actually take effect. Without it, hp keeps
    // the value Awake copied and you get nonsense like 150/10.
    private void OnValidate()
    {
        if (!Application.isPlaying) return;

        hp = Mathf.Clamp(hp, 0, Mathf.Max(1, maxHp));
        OnHealthChanged?.Invoke(Normalized);
    }

    // For the debug HUD. Goes through the same death path a real killing blow does,
    // so testing the ending cannot diverge from the real thing.
    public void DebugSetHealth(int value)
    {
        if (state.IsDead) return;

        hp = Mathf.Clamp(value, 0, maxHp);
        OnHealthChanged?.Invoke(Normalized);

        if (hp <= 0) StartCoroutine(Die());
    }

    private void Update()
    {
        if (state.IsDead || !isInvincible) return;

        iFrameTimer -= Time.deltaTime;
        if (iFrameTimer <= 0f) isInvincible = false;
    }

    public bool TakeDamage(int damage, Vector2 attackerPos)
    {
        // Untouchable on the throne, so a stray swing cannot start the fight by accident.
        if (state.IsDead || isInvincible) return false;
        if (state.CurrentState == KingState.KingStateType.Throne) return false;

        hp -= damage;
        isInvincible = true;
        iFrameTimer = iFrameTime;

        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(HitFlash());

        // No stagger and no state change. He is hit, and he does not care.
        OnHit?.Invoke();
        OnHealthChanged?.Invoke(Normalized);

        if (hp <= 0)
        {
            hp = 0;
            StartCoroutine(Die());
            return true;
        }

        return false;
    }

    private IEnumerator Die()
    {
        state.IsDead = true;

        if (brain != null) brain.Deactivate();

        state.CurrentState = KingState.KingStateType.Dead;

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        OnDied?.Invoke();

        yield return new WaitForSeconds(deathDelay);

        Destroy(gameObject);
    }

    private IEnumerator HitFlash()
    {
        if (mat == null) yield break;

        float half = hitFlashTime * 0.5f;

        for (int phase = 0; phase < 2; phase++)
        {
            float t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                float k = t / half;
                mat.SetFloat("_FlashAmount", phase == 0 ? k : 1f - k);
                yield return null;
            }
        }

        mat.SetFloat("_FlashAmount", 0f);
    }
}
