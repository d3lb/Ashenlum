using UnityEngine;
using System.Collections;

public class BreakableMoneyBox : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Collider2D boxCollider;
    [SerializeField] private LumenDropper lumenDropper;

    [Header("Health")]
    [SerializeField] private int maxHp = 1;

    [Header("Visual")]
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float hitFlashTime = 0.08f;
    [SerializeField] private float breakDelay = 0.05f;

    private int hp;
    private bool isBroken;
    private Color originalColor;

    private void Awake()
    {
        hp = maxHp;
        if (sprite != null) originalColor = sprite.color;
    }

    public bool TakeDamage(int damage, Vector2 attackerPosition)
    {
        if (isBroken) return false;

        hp -= damage;
        StartCoroutine(HitFlash());

        if (hp <= 0)
        {
            StartCoroutine(BreakRoutine());
            return true;
        }

        return false;
    }

    private IEnumerator HitFlash()
    {
        if (sprite == null) yield break;

        sprite.color = hitColor;
        yield return new WaitForSeconds(hitFlashTime);

        if (!isBroken)
            sprite.color = originalColor;
    }

    private IEnumerator BreakRoutine()
    {
        isBroken = true;

        if (boxCollider != null) boxCollider.enabled = false;
        if (sprite != null) sprite.color = hitColor;

        yield return new WaitForSeconds(breakDelay);

        if (lumenDropper != null)
            lumenDropper.Drop();

        Destroy(gameObject);
    }
}