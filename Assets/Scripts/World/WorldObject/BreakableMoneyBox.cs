using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class BreakableMoneyBox : MonoBehaviour, IDamageable {
    [Header("References")]
    private PersistentObject persistentObject;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Collider2D boxCollider;
    [SerializeField] private LumenDropper lumenDropper;

    // Pristine first, most damaged last. One per hit, and the last hit breaks it - so four
    // sprites means four hits. Damage amount is ignored on purpose: the player hits for 2 to
    // 5 depending on stability, which would make it break in a different number of hits
    // from one room to the next.
    [Header("Damage stages")]
    [SerializeField] private Sprite[] stages;

    [Header("Visual")]
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float hitFlashTime = 0.08f;
    [SerializeField] private float breakDelay = 0.05f;

    // Dark until struck, so the burst is the only light it ever gives off.
    [Header("Hit light")]
    [SerializeField] private Light2D light2D;
    [SerializeField] private float burstIntensity = 3f;
    [SerializeField] private float burstIn = 0.04f;
    [SerializeField] private float burstOut = 0.35f;

    // A child system, replayed per hit. Needs Play On Awake off.
    [Header("Hit particles")]
    [SerializeField] private ParticleSystem hitParticles;

    [Header("Hit shake")]
    [SerializeField] private float shakeTime = 0.12f;
    [SerializeField] private float shakeAmount = 0.06f;

    private int hits;
    private bool isBroken;
    private Color originalColor;

    // Captured once. Re-reading it per hit would bake a shaken offset into the rest pose.
    private Vector3 spriteHome;
    private Coroutine shakeRoutine;
    private Coroutine burstRoutine;

    private void Awake() {
        // Unassigned, every visual here silently did nothing: stage, flash and shake all
        // start by returning when it is null.
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>(true);
        if (hitParticles == null) hitParticles = GetComponentInChildren<ParticleSystem>(true);

        if (sprite != null) {
            originalColor = sprite.color;
            spriteHome = sprite.transform.localPosition;
        }

        persistentObject = GetComponent<PersistentObject>();

        if (light2D == null) light2D = GetComponentInChildren<Light2D>(true);
        if (light2D != null) light2D.intensity = 0f;

        ShowStage();
    }

    private void Start() {
        if (persistentObject == null)
            return;

        if (GameManager.Instance.activeRun.permanentRemoved.Contains(persistentObject.Id)) {
            Destroy(gameObject);
        }
    }

    public bool TakeDamage(int damage, Vector2 attackerPosition) {
        if (isBroken) return false;

        hits++;

        ShowStage();
        StartCoroutine(HitFlash());

        // Restarted, not stacked: two hits close together would fight over the position.
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(Shake());

        if (burstRoutine != null) StopCoroutine(burstRoutine);
        burstRoutine = StartCoroutine(Burst());

        // Play, not Emit: restarts the whole system so each hit reads as its own burst.
        if (hitParticles != null) hitParticles.Play();

        // No stages set means it is a one-hit box, which is how it behaved before.
        if (stages == null || stages.Length == 0 || hits >= stages.Length) {
            StartCoroutine(BreakRoutine());
            return true;
        }

        return false;
    }

    // Index is the hit count, so stage 0 is the untouched box and the last one never shows
    // for long - that hit is the one that breaks it.
    private void ShowStage() {
        if (sprite == null || stages == null || stages.Length == 0) return;

        int index = Mathf.Clamp(hits, 0, stages.Length - 1);

        if (stages[index] != null) sprite.sprite = stages[index];
    }

    // Snaps up, eases down: the reverse reads as a lamp turning on rather than a strike.
    private IEnumerator Burst() {
        if (light2D == null) yield break;

        float from = light2D.intensity;
        float t = 0f;

        while (t < burstIn) {
            t += Time.deltaTime;
            light2D.intensity = Mathf.Lerp(from, burstIntensity, t / burstIn);
            yield return null;
        }

        t = 0f;

        while (t < burstOut) {
            t += Time.deltaTime;
            light2D.intensity = Mathf.Lerp(burstIntensity, 0f, t / burstOut);
            yield return null;
        }

        light2D.intensity = 0f;
        burstRoutine = null;
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

    private IEnumerator HitFlash() {
        if (sprite == null) yield break;

        sprite.color = hitColor;
        yield return new WaitForSeconds(hitFlashTime);

        if (!isBroken)
            sprite.color = originalColor;
    }

    private IEnumerator BreakRoutine() {
        isBroken = true;

        GameManager.Instance.activeRun.permanentRemoved.Add(persistentObject.Id);
        if (boxCollider != null) boxCollider.enabled = false;
        if (sprite != null) sprite.color = hitColor;

        yield return new WaitForSeconds(breakDelay);

        if (lumenDropper != null)
            lumenDropper.Drop();

        // Cut loose first, or the burst from the killing blow is destroyed the frame it starts.
        if (hitParticles != null) {
            hitParticles.transform.SetParent(null, true);
            Destroy(hitParticles.gameObject, 3f);
        }

        Destroy(gameObject);
    }
}
