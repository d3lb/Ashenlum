using System.Collections;
using UnityEngine;

// What is left of you where you fell. One hit opens it and everything you were carrying
// comes back. It is not an enemy - it has no health and it does not fight back. The walk
// to reach it is the punishment, not a second fight.
public class PlayerShade : MonoBehaviour, IDamageable
{
    [Header("References")]
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Collider2D     shadeCollider;
    [SerializeField] private LumenPickup    lumenPrefab;

    [Header("Payout")]
    [Tooltip("A whole purse is split across at most this many pickups.")]
    [SerializeField] private int   maxPickups    = 8;
    [SerializeField] private float scatterRadius = 0.4f;

    [Header("Visual")]
    [SerializeField] private Color hitColor   = Color.white;
    [SerializeField] private float breakDelay = 0.05f;

    private bool isBroken;

    // One hit opens it, whatever the damage. Making the player grind it down would only
    // pad the walk back with busywork.
    public bool TakeDamage(int damage, Vector2 attackerPosition)
    {
        if (isBroken) return false;

        StartCoroutine(BreakRoutine());
        return true;
    }

    private IEnumerator BreakRoutine()
    {
        isBroken = true;

        if (shadeCollider != null) shadeCollider.enabled = false;
        if (sprite != null)        sprite.color = hitColor;

        yield return new WaitForSeconds(breakDelay);

        Payout();

        // Clear the record before the object goes, so reloading the scene cannot put back
        // a shade the player already opened.
        GameManager.Instance.CollectShade();

        Destroy(gameObject);
    }

    private void Payout()
    {
        int total = GameManager.Instance.activeRun.droppedLumens;
        if (total <= 0 || lumenPrefab == null) return;

        int count = Mathf.Min(total, Mathf.Max(1, maxPickups));
        int each  = total / count;

        // Spread the remainder one per pickup so rounding never eats a lumen.
        int extra = total % count;

        for (int i = 0; i < count; i++)
        {
            Vector2 pos = (Vector2)transform.position + Random.insideUnitCircle * scatterRadius;

            LumenPickup pickup = Instantiate(lumenPrefab, pos, Quaternion.identity);
            pickup.SetValue(each + (i < extra ? 1 : 0));
        }
    }
}
