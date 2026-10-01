using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class KingHealth : MonoBehaviour, IDamageable {
    [Header("Health")]
    [SerializeField] private int maxHp = 90;

    [Header("Flash")]
    [SerializeField] private float hitFlashTime = 0.15f;

    [Header("Invincibility")]
    [SerializeField] private float iFrameTime = 0.1f;

    [Header("Death")]
    [SerializeField] private float whiteOutTime = 0.6f;
    [SerializeField] private float whiteHold = 0.15f;
    [SerializeField] private float deathDelay = 1.2f;

    // The ash. Unparented, so it outlives him.
    [SerializeField] private GameObject deathEffect;

    // An empty child placed over his body. The root pivot is rarely where he looks like he is.
    [SerializeField] private Transform deathEffectPoint;

    [Header("References")]
    [SerializeField] private KingState state;
    [SerializeField] private KingBrain brain;
    [SerializeField] private SpriteRenderer sprite;

    private int hp;
    private float iFrameTimer;
    private bool isInvincible;
    private Material mat;
    private Coroutine flashCoroutine;

    public int CurrentHP => hp;
    public int MaxHP => maxHp;
    public float Normalized => maxHp <= 0 ? 0f : Mathf.Clamp01((float)hp / maxHp);

    public System.Action<float> OnHealthChanged;
    public System.Action OnDied;

    // Drives the brain's aggression counter.
    public System.Action OnHit;

    private void Awake() {
        hp = maxHp;

        if (state == null)  state  = GetComponent<KingState>();
        if (brain == null)  brain  = GetComponent<KingBrain>();
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();

        if (sprite != null)
            mat = sprite.material = new Material(sprite.material);
    }

    // Without this, editing maxHp during Play leaves hp at the value Awake copied.
    private void OnValidate() {
        if (!Application.isPlaying) return;

        hp = Mathf.Clamp(hp, 0, Mathf.Max(1, maxHp));
        OnHealthChanged?.Invoke(Normalized);
    }

    // Debug HUD. Uses the real death path so testing cannot diverge from it.
    public void DebugSetHealth(int value) {
        if (state.IsDead) return;

        hp = Mathf.Clamp(value, 0, maxHp);
        OnHealthChanged?.Invoke(Normalized);

        if (hp <= 0) StartCoroutine(Die());
    }

    private void Update() {
        if (state.IsDead || !isInvincible) return;

        iFrameTimer -= Time.deltaTime;
        if (iFrameTimer <= 0f) isInvincible = false;
    }

    public bool TakeDamage(int damage, Vector2 attackerPos) {
        // Untouchable on the throne.
        if (state.IsDead || isInvincible) return false;
        if (state.CurrentState == KingState.KingStateType.Throne) return false;

        hp -= damage;
        isInvincible = true;
        iFrameTimer = iFrameTime;

        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(HitFlash());

        // No stagger, no state change.
        OnHit?.Invoke();
        OnHealthChanged?.Invoke(Normalized);

        if (hp <= 0) {
            hp = 0;
            StartCoroutine(Die());
            return true;
        }

        return false;
    }

    private IEnumerator Die() {
        state.IsDead = true;

        if (brain != null) brain.Deactivate();

        // Deactivate stops the brain, not the beams it already put in the air. Without this
        // the killing blow can be answered by a telegraph that was mid-count.
        KingLight.KillAll();

        state.CurrentState = KingState.KingStateType.Dead;

        GameManager.Instance?.CountKill();

        // He stands there through his last words. The encounter calls Burn when they end.
        if (OnDied != null) OnDied.Invoke();
        else yield return Burn();
    }

    // Driven by whoever owns the ending, so it cannot start before the dialogue is done.
    public IEnumerator Burn() {
        yield return WhiteOut();

        HideVisuals();

        if (deathEffect != null) {
            Transform at = deathEffectPoint != null ? deathEffectPoint : transform;
            Instantiate(deathEffect, at.position, at.rotation);
        }
        else
            Debug.LogError($"[KingHealth] '{name}' has no Death Effect, so he leaves nothing.", this);

        yield return new WaitForSeconds(deathDelay);

        Destroy(gameObject);
    }

    // Burns out rather than flashing: one ramp to full white with no fade back.
    private IEnumerator WhiteOut() {
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = null;

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);

        for (float t = 0f; t < whiteOutTime; t += Time.deltaTime) {
            SetFlash(renderers, Mathf.Clamp01(t / whiteOutTime));
            yield return null;
        }

        SetFlash(renderers, 1f);

        if (whiteHold > 0f) yield return new WaitForSeconds(whiteHold);
    }

    // Skips anything on a material without the property, so the staff star and any sprite on
    // a plain material simply do not take part.
    private static void SetFlash(SpriteRenderer[] renderers, float amount) {
        foreach (SpriteRenderer r in renderers) {
            if (r == null) continue;

            Material m = r.material;
            if (m.HasProperty("_FlashAmount")) m.SetFloat("_FlashAmount", amount);
        }
    }

    // He does not fade, he is simply gone the moment the ash starts.
    private void HideVisuals() {
        foreach (SpriteRenderer r in GetComponentsInChildren<SpriteRenderer>(true))
            r.enabled = false;

        foreach (Light2D l in GetComponentsInChildren<Light2D>(true))
            l.enabled = false;
    }

    private IEnumerator HitFlash() {
        if (mat == null) yield break;

        float half = hitFlashTime * 0.5f;

        for (int phase = 0; phase < 2; phase++) {
            float t = 0f;
            while (t < half) {
                t += Time.deltaTime;
                float k = t / half;
                mat.SetFloat("_FlashAmount", phase == 0 ? k : 1f - k);
                yield return null;
            }
        }

        mat.SetFloat("_FlashAmount", 0f);
    }
}
