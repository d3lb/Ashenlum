using System.Collections;
using UnityEngine;

// What is left of you where you fell. Two hits crack it open and your light comes back
// out already heading for you. It has no health bar and it does not fight back.
public class PlayerShade : MonoBehaviour, IDamageable
{
    [Header("References")]
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Collider2D     shadeCollider;
    [SerializeField] private LumenPickup    lumenPrefab;

    [Header("Breaking")]
    [SerializeField] private int   hitsToBreak = 2;
    [SerializeField] private float breakDelay  = 0.05f;

    [Header("Payout")]
    [SerializeField] private int   maxPickups    = 8;
    [SerializeField] private float scatterRadius = 0.4f;

    [Header("Visual")]
    [SerializeField] private Color hitColor  = Color.white;
    [SerializeField] private float flashTime = 0.08f;

    private Color baseColor;
    private int   hitsTaken;
    private bool  isBroken;

    private void Awake()
    {
        if (sprite != null) baseColor = sprite.color;
    }

    // Damage amount is ignored on purpose - a stronger weapon should not open your own
    // light faster, it is a fixed two hits so the cost of dying never changes.
    public bool TakeDamage(int damage, Vector2 attackerPosition)
    {
        if (isBroken) return false;

        hitsTaken++;

        if (hitsTaken < hitsToBreak)
        {
            StartCoroutine(Flash());
            return true;
        }

        StartCoroutine(BreakRoutine());
        return true;
    }

    private IEnumerator Flash()
    {
        if (sprite == null) yield break;

        sprite.color = hitColor;
        yield return new WaitForSeconds(flashTime);
        sprite.color = baseColor;
    }

    private IEnumerator BreakRoutine()
    {
        isBroken = true;

        if (shadeCollider != null) shadeCollider.enabled = false;
        if (sprite != null)        sprite.color = hitColor;

        yield return new WaitForSeconds(breakDelay);

        // Never clear the record for a payout that did not happen - that would destroy
        // the shade and the lumens with it.
        if (!Payout())
        {
            Debug.LogError("[PlayerShade] Nothing was paid out, so the shade is staying. " +
                           "Check the Lumen Prefab field.", this);

            isBroken = false;
            hitsTaken = 0;
            if (shadeCollider != null) shadeCollider.enabled = true;
            if (sprite != null)        sprite.color = baseColor;
            yield break;
        }

        // Clear the record before the object goes, so reloading the scene cannot put back
        // a shade the player already opened.
        GameManager.Instance.CollectShade();

        Destroy(gameObject);
    }

    private bool Payout()
    {
        if (GameManager.Instance == null || lumenPrefab == null) return false;

        int total = GameManager.Instance.activeRun.droppedLumens;
        if (total <= 0) return false;

        int count = Mathf.Min(total, Mathf.Max(1, maxPickups));
        int each  = total / count;

        // Spread the remainder one per pickup so rounding never eats a lumen.
        int extra = total % count;

        for (int i = 0; i < count; i++)
        {
            Vector2 pos = (Vector2)transform.position + Random.insideUnitCircle * scatterRadius;

            LumenPickup pickup = Instantiate(lumenPrefab, pos, Quaternion.identity);
            pickup.SetValue(each + (i < extra ? 1 : 0));

            // Straight to the player - this light is already theirs, it should not sit
            // on the floor waiting to be walked over.
            pickup.LaunchAtPlayer();
        }

        return true;
    }
}
