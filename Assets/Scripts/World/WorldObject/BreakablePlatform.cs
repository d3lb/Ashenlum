using UnityEngine;
using System.Collections;

public class BreakablePlatform : MonoBehaviour {
    [Header("References")]
    [SerializeField] private Collider2D groundCollider;
    [SerializeField] private Collider2D topTrigger;

    // Bottom first. They fall away in this order, and the collider goes with the last one.
    [Header("Layers")]
    [SerializeField] private Transform[] layers;

    [Header("Settings")]
    [SerializeField] private float breakDelay = 0.5f;
    [SerializeField] private float respawnDelay = 3f;
    [SerializeField] private bool respawn = true;

    [Header("Layer fall")]
    [SerializeField] private float fallTime = 0.6f;
    [SerializeField] private float fallSpeed = 4f;

    private bool isBreaking;

    private Vector3[] homes;
    private SpriteRenderer[] art;
    private Color[] colors;

    private void Awake() {
        if (layers == null) return;

        homes = new Vector3[layers.Length];
        art = new SpriteRenderer[layers.Length];
        colors = new Color[layers.Length];

        for (int i = 0; i < layers.Length; i++) {
            if (layers[i] == null) continue;

            homes[i] = layers[i].localPosition;
            art[i] = layers[i].GetComponent<SpriteRenderer>();

            if (art[i] != null) colors[i] = art[i].color;
        }
    }

    public void TriggerBreak() {
        if (isBreaking) return;
        StartCoroutine(BreakRoutine());
    }

    private IEnumerator BreakRoutine() {
        isBreaking = true;
        topTrigger.enabled = false;

        int count = layers != null ? layers.Length : 0;

        // Spread across breakDelay, so the timing is set by one number no matter how many
        // layers the platform is built from.
        float interval = count > 0 ? breakDelay / count : breakDelay;

        for (int i = 0; i < count; i++) {
            // On the last one, the floor goes as it starts falling - the top layer takes the
            // ground with it rather than the ground vanishing a beat later.
            if (i == count - 1) groundCollider.enabled = false;

            StartCoroutine(Drop(i));

            if (i < count - 1) yield return new WaitForSeconds(interval);
        }

        if (count == 0) {
            yield return new WaitForSeconds(breakDelay);
            groundCollider.enabled = false;
        }

        if (!respawn) yield break;

        // The last layer is still in the air; restoring sooner would snatch it back mid-fall.
        yield return new WaitForSeconds(Mathf.Max(respawnDelay, fallTime));

        Restore();

        groundCollider.enabled = true;
        topTrigger.enabled = true;

        isBreaking = false;
    }

    private IEnumerator Drop(int index) {
        Transform layer = layers[index];
        if (layer == null) yield break;

        Vector3 home = homes[index];
        SpriteRenderer sprite = art[index];
        Color start = colors[index];

        float t = 0f;

        while (t < fallTime) {
            t += Time.deltaTime;

            // Squared, so it accelerates away instead of sliding down at a constant rate.
            layer.localPosition = home + Vector3.down * (fallSpeed * t * t);

            if (sprite != null) {
                Color c = start;
                c.a = start.a * (1f - (t / fallTime));
                sprite.color = c;
            }

            yield return null;
        }

        layer.gameObject.SetActive(false);
    }

    private void Restore() {
        if (layers == null) return;

        for (int i = 0; i < layers.Length; i++) {
            if (layers[i] == null) continue;

            layers[i].localPosition = homes[i];

            if (art[i] != null) art[i].color = colors[i];

            layers[i].gameObject.SetActive(true);
        }
    }
}
