using UnityEngine;
using System.Collections;

public class BreakableWall : MonoBehaviour, IDamageable {
    [SerializeField] private string wallID;

    [Header("References")]
    [SerializeField] private SpriteRenderer sprite;

    // A child system, replayed per hit. Needs Play On Awake off.
    [SerializeField] private ParticleSystem hitParticles;

    // Pristine first, most damaged last. One per hit, and the last hit breaks it - so three
    // sprites means three hits. The damage amount is ignored: the player hits for 2 to 5
    // depending on stability, which would make it break in a different number of hits.
    [Header("Damage stages")]
    [SerializeField] private Sprite[] stages;

    [Header("Hit shake")]
    [SerializeField] private float shakeTime = 0.12f;
    [SerializeField] private float shakeAmount = 0.06f;

    private int hits;
    private bool isBroken;

    // Captured once. Re-reading it per hit would bake a shaken offset into the rest pose.
    private Vector3 spriteHome;
    private Coroutine shakeRoutine;

    private void Awake() {
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>(true);
        if (hitParticles == null) hitParticles = GetComponentInChildren<ParticleSystem>(true);

        if (sprite != null) spriteHome = sprite.transform.localPosition;

        ShowStage();
    }

    private void Start() {
        if (GameManager.Instance.IsWallBroken(wallID)) Destroy(gameObject);
    }

    public bool TakeDamage(int damage, Vector2 attackerPos) {
        // Two hitboxes landing on the same frame would otherwise register the break twice.
        if (isBroken) return false;

        hits++;

        ShowStage();

        // Restarted, not stacked: two hits close together would fight over the position.
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(Shake());

        // Play, not Emit: restarts the whole system so each hit reads as its own burst.
        if (hitParticles != null) hitParticles.Play();

        if (stages == null || stages.Length == 0 || hits >= stages.Length) {
            Break();
            return true;
        }

        return false;
    }

    private void Break() {
        isBroken = true;

        GameManager.Instance.RegisterBrokenWall(wallID);

        // Cut loose first, or the burst from the killing blow dies the frame it starts.
        if (hitParticles != null) {
            hitParticles.transform.SetParent(null, true);
            Destroy(hitParticles.gameObject, 3f);
        }

        Destroy(gameObject);
    }

    // Index is the hit count, so stage 0 is the untouched wall.
    private void ShowStage() {
        if (sprite == null || stages == null || stages.Length == 0) return;

        int index = Mathf.Clamp(hits, 0, stages.Length - 1);

        if (stages[index] != null) sprite.sprite = stages[index];
    }

    private IEnumerator Shake() {
        if (sprite == null) yield break;

        Transform art = sprite.transform;
        float elapsed = 0f;

        while (elapsed < shakeTime) {
            elapsed += Time.deltaTime;

            // Fades out over the shake, so it settles instead of stopping dead.
            float falloff = 1f - (elapsed / shakeTime);
            art.localPosition = spriteHome + (Vector3)(Random.insideUnitCircle * shakeAmount * falloff);

            yield return null;
        }

        art.localPosition = spriteHome;
        shakeRoutine = null;
    }
}
