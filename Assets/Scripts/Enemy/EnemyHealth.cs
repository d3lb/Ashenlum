using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private Material whiteFlashMat;
    [Space(5)]

    [Header("Health")]
    [SerializeField] private int hp = 6;
    [SerializeField] private int maxHp = 6;

    [Header("Knockback")]
    [SerializeField] private bool knockbackable = true;
    [SerializeField] private float knockbackStrength = 5f;
    [SerializeField] private float knockbackTime = 0.2f;
    private float knockbackTimer;

    [Header("Flash")]
    [SerializeField] private float hitFlashTime = 0.15f;

    [Header("Invincibility")]
    [SerializeField] private float iFrameTime = 0.1f;
    private float iFrameTimer;
    private bool isInvincible;

    private Material mat;
    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private EnemyState state;

    private Coroutine flashCoroutine; 

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        state = GetComponent<EnemyState>();
        sprite = GetComponent<SpriteRenderer>();
        mat = sprite.material = new Material(sprite.material);
    }
    public void Update()
    {
        if (isInvincible)
        {
            iFrameTimer -= Time.deltaTime;
            if (iFrameTimer <= 0)
                isInvincible = false;
        }

        if (state.IsKnocked)
        {
            knockbackTimer -= Time.deltaTime;

            if (knockbackTimer <= 0)
            {
                state.IsKnocked = false;
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }
    } 
    public void TakeDamage(int dmg, Vector2 attackerPos)
    {
        if (isInvincible)
            return;

        hp -= dmg;

        isInvincible = true;
        iFrameTimer = iFrameTime;

        // Knockback
        if (knockbackable)
        {
            Vector2 dir = (transform.position - (Vector3)attackerPos).normalized;

            rb.AddForce(dir * knockbackStrength, ForceMode2D.Impulse);

            knockbackTimer = knockbackTime;
            state.CurrentState = EnemyState.EnemyStateType.Hit;
            state.IsKnocked = true;
        }
        
        // Flash
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(HitFlash());


        // call death if hp <= 0
        if (hp <= 0)
        {
            Die();
        }
    }


    // death
    private void Die()
    {
        Destroy(gameObject);
    }


    // white flash when hit
    private IEnumerator HitFlash()
    {
        float halfTime = hitFlashTime * 0.5f;
        float timer = 0f;

        // fade in
        while (timer < halfTime)
        {
            timer += Time.deltaTime;
            float t = timer / halfTime; // 0 -> 1
            mat.SetFloat("_FlashAmount", t);
            yield return null;
        }

        timer = 0f;

        // fade out
        while (timer < halfTime)
        {
            timer += Time.deltaTime;
            float t = 1f - (timer / halfTime); // 1 -> 0
            mat.SetFloat("_FlashAmount", t);
            yield return null;
        }

        mat.SetFloat("_FlashAmount", 0f);
    }
}