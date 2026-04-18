using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private Material whiteFlashMat;
    [Space(5)]
    [SerializeField] private int hp = 6;
    [SerializeField] private float knockbackStrength = 2f;
    [SerializeField] private float hitFlashTime = 0.15f;
    private bool isKnocked;
    [SerializeField] private float knockbackTime = 0.2f;
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
        if (isKnocked && Mathf.Abs(rb.linearVelocity.x) < 0.1f)
        {
            isKnocked = false;
        }
    }
    public void TakeDamage(int dmg, Vector2 attackerPos)
    {
        hp -= dmg;

        // Knockback
        isKnocked = true;
        Vector2 dir = (transform.position - (Vector3)attackerPos).normalized;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        rb.AddForce(dir * knockbackStrength, ForceMode2D.Impulse);

        // Flash
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(HitFlash());


        if (hp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }

    private IEnumerator HitFlash()
    {
        sprite.material = whiteFlashMat;
        yield return new WaitForSeconds(hitFlashTime);
        sprite.material = originalMat;
    }
}