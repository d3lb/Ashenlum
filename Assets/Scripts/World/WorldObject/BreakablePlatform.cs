using UnityEngine;
using System.Collections;

public class BreakablePlatform : MonoBehaviour {
    [Header("References")]
    [SerializeField] private Collider2D groundCollider;
    [SerializeField] private Collider2D topTrigger;
    [SerializeField] private SpriteRenderer sprite;

    [Header("Settings")]
    [SerializeField] private float breakDelay = 0.5f;
    [SerializeField] private float respawnDelay = 3f;
    [SerializeField] private bool respawn = true;

    [Header("Visual")]
    [SerializeField] private Color warningColor = Color.red;

    private bool isBreaking;
    private Color originalColor;

    private void Awake() {
        if (sprite != null) originalColor = sprite.color;
    }

    public void TriggerBreak() {
        if (isBreaking) return;
        StartCoroutine(BreakRoutine());
    }

    private IEnumerator BreakRoutine() {
        isBreaking = true;
        topTrigger.enabled = false;

        float timer = 0f;

        while (timer < breakDelay) {
            timer += Time.deltaTime;
            if (sprite != null) sprite.color = Color.Lerp(originalColor, warningColor, timer / breakDelay);
            yield return null;
        }

        groundCollider.enabled = false;

        if (sprite != null) sprite.enabled = false;

        if (!respawn) yield break;

        yield return new WaitForSeconds(respawnDelay);

        groundCollider.enabled = true;
        topTrigger.enabled = true;

        if (sprite != null) {
            sprite.color = originalColor;
            sprite.enabled = true;
        }

        isBreaking = false;
    }
}