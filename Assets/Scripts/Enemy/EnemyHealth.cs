using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private Material whiteFlashMat;
    [Space(5)]
    [SerializeField] private int hp = 6;
    [SerializeField] private float knockbackStrength = 5f;
    [SerializeField] private float hitFlashTime = 0.15f;
    private bool isKnocked;
    [SerializeField] private float knockbackTime = 0.2f;
    private float knockbackTimer;
    public bool _isKnocked => isKnocked;

    private Material originalMat;
    private Rigidbody2D rb;
    private SpriteRenderer sprite;

    private Coroutine flashCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        originalMat = sprite.material;

    }
    public void Update()
    {
        if (isKnocked)
        {
            knockbackTimer -= Time.deltaTime;

            if (knockbackTimer <= 0)
            {
                isKnocked = false;
            }
        }
    } 
    public void TakeDamage(int dmg, Vector2 attackerPos)
    {
        hp -= dmg;

        // Knockback
        Vector2 dir = (transform.position - (Vector3)attackerPos).normalized;
        
        rb.AddForce(dir * knockbackStrength, ForceMode2D.Impulse);

        knockbackTimer = knockbackTime;
        isKnocked = true;

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
        sprite.material = whiteFlashMat;
        yield return new WaitForSeconds(hitFlashTime);
        sprite.material = originalMat;
    }
}