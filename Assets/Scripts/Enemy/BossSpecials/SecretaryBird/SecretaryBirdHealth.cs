using System.Collections;
using UnityEngine;

/// <summary>
/// Health, i-frames, hit flash, and the death handoff.
/// Exposes Normalized so the brain can drive phases off it.
/// </summary>
public class SecretaryBirdHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHp = 30;

    [Header("Flash")]
    [SerializeField] private float hitFlashTime = 0.15f;

    [Header("Invincibility")]
    [SerializeField] private float iFrameTime = 0.1f;

    [Header("Death")]
    [SerializeField] private float deathDelay = 1.2f;
    [SerializeField] private GameObject deathEffect;

    [Header("References")]
    [SerializeField] private SecretaryBirdState state;
    [SerializeField] private SecretaryBirdBrain brain;
    [SerializeField] private SpriteRenderer sprite;

    private int hp;
    private float iFrameTimer;
    private bool isInvincible;
    private Material mat;
    private Coroutine flashCoroutine;

    public int CurrentHP => hp;
    public int MaxHP => maxHp;
    public float Normalized => maxHp <= 0 ? 0f : Mathf.Clamp01((float)hp / maxHp);

    /// <summary>Hook the health bar / arena door / reward to these.</summary>
    public System.Action<float> OnHealthChanged;
    public System.Action OnDied;

    private void Awake()
    {
        hp = maxHp;

        if (state == null)  state  = GetComponent<SecretaryBirdState>();
        if (brain == null)  brain  = GetComponent<SecretaryBirdBrain>();
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();

        if (sprite != null)
            mat = sprite.material = new Material(sprite.material);
    }

    private void Update()
    {
        if (state.IsDead || !isInvincible) return;

        iFrameTimer -= Time.deltaTime;
        if (iFrameTimer <= 0f) isInvincible = false;
    }

    public bool TakeDamage(int damage, Vector2 attackerPos)
    {
        if (state.IsDead || isInvincible) return false;

        hp -= damage;
        isInvincible = true;
        iFrameTimer = iFrameTime;

        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(HitFlash());

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

        // Stop the fight FIRST so no coroutine is mid-dash when the object goes away.
        if (brain != null) brain.Deactivate();

        state.CurrentState = SecretaryBirdState.BossStateType.Dead;

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        OnDied?.Invoke();

        // TODO: death animation, unlock arena, drop reward, persist "boss defeated"
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
