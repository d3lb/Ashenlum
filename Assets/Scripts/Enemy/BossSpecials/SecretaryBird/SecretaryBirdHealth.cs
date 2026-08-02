using System.Collections;
using UnityEngine;

public class SecretaryBirdHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int hp = 30;

    [Header("Flash")]
    [SerializeField] private float hitFlashTime = 0.15f;

    [Header("Invincibility")]
    [SerializeField] private float iFrameTime = 0.1f;

    private float iFrameTimer;
    private bool isInvincible;

    private Material mat;
    private SpriteRenderer sprite;
    private SecretaryBirdState state;

    private Coroutine flashCoroutine;

    private void Awake()
    {
        state = GetComponent<SecretaryBirdState>();

        sprite = GetComponent<SpriteRenderer>();
        mat = sprite.material = new Material(sprite.material);
    }

    private void Update()
    {
        if (state.IsDead)
            return;

        if (isInvincible)
        {
            iFrameTimer -= Time.deltaTime;

            if (iFrameTimer <= 0f)
                isInvincible = false;
        }
    }

    public bool TakeDamage(int damage, Vector2 attackerPos)
    {
        if (state.IsDead || isInvincible)
            return false;

        hp -= damage;

        isInvincible = true;
        iFrameTimer = iFrameTime;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(HitFlash());

        if (hp <= 0)
        {
            Die();
            return true;
        }

        return false;
    }

    private void Die()
    {
        state.IsDead = true;
        state.CurrentState = SecretaryBirdState.BossStateType.Dead;

        // Play death animation
        // Unlock arena
        // Give reward
        // Save boss defeated

        Destroy(gameObject);
    }

    private IEnumerator HitFlash()
    {
        float halfTime = hitFlashTime * 0.5f;
        float timer = 0f;

        while (timer < halfTime)
        {
            timer += Time.deltaTime;
            mat.SetFloat("_FlashAmount", timer / halfTime);
            yield return null;
        }

        timer = 0f;

        while (timer < halfTime)
        {
            timer += Time.deltaTime;
            mat.SetFloat("_FlashAmount", 1f - (timer / halfTime));
            yield return null;
        }

        mat.SetFloat("_FlashAmount", 0f);
    }
}