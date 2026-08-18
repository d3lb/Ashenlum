using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    private PersistentObject persistentObject;

    [SerializeField] private GameObject corpsePrefab;
    [Space(5)]

    [Header("Health")]
    [SerializeField] private int hp = 6;

    [Header("Knockback")]
    [SerializeField] private bool knockbackable = true;
    [SerializeField] private float knockbackStrength = 5f;
    [SerializeField] private float knockbackTime = 0.2f;
    private float knockbackTimer;

    [Header("Flash")]
    [SerializeField] private float hitFlashTime = 0.15f;

    [Header("Invincibility")]
    [SerializeField] private float iFrameTime = 0.1f;
    private float iFrameTimer;
    private bool isInvincible;

    private Material mat;
    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private EnemyState state;

    private Coroutine flashCoroutine;


    private Vector2 lastHitDirection;

    private Vector3 spawnPosition;
    private int startHp;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        state = GetComponent<EnemyState>();
        sprite = GetComponent<SpriteRenderer>();
        mat = sprite.material = new Material(sprite.material);
        persistentObject = GetComponent<PersistentObject>();

        spawnPosition = transform.position;
        startHp = hp;

        WorldReset.Register(this);
    }

    private void OnDestroy() => WorldReset.Unregister(this);

    private void Start()
    {
        if (persistentObject == null)
            return;

        GameRunProfile run = GameManager.Instance.activeRun;

        // Permanent is gone for good. Temporary only sleeps - a rest brings it back.
        if (run.permanentRemoved.Contains(persistentObject.Id))
            Destroy(gameObject);
        else if (run.temporaryRemoved.Contains(persistentObject.Id))
            gameObject.SetActive(false);
    }

    public void ResetToSpawn()
    {
        if (gameObject.activeSelf && hp >= startHp) return;

        GameRunProfile run = GameManager.Instance != null ? GameManager.Instance.activeRun : null;

        // Permanent kills stay dead - that is the whole point of marking them permanent.
        // The temporary set is emptied by WorldReset, not one id at a time.
        if (persistentObject != null && run != null &&
            run.permanentRemoved.Contains(persistentObject.Id)) return;

        hp = startHp;
        isInvincible = false;
        iFrameTimer = 0f;
        knockbackTimer = 0f;

        transform.position = spawnPosition;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (state != null)
        {
            state.IsDead = false;
            state.IsKnocked = false;
            state.IsAttacking = false;
            state.CurrentState = EnemyState.EnemyStateType.Patrol;
        }

        flashCoroutine = null;
        if (mat != null) mat.SetFloat("_FlashAmount", 0f);

        // Active BEFORE the reset call: an implementor is allowed to restart a coroutine
        // in there, and StartCoroutine throws on an inactive object.
        gameObject.SetActive(true);

        // Whatever a killed coroutine never got to undo.
        foreach (IRespawnReset part in GetComponentsInChildren<IRespawnReset>(true))
            part.ResetForRespawn();
    }

    public void Update()
    {
        if (state.IsDead)
            return;

        if (isInvincible)
        {
            iFrameTimer -= Time.deltaTime;
            if (iFrameTimer <= 0)
                isInvincible = false;
        }

        if (state.IsKnocked)
        {
            knockbackTimer -= Time.deltaTime;

            if (knockbackTimer <= 0)
            {
                state.IsKnocked = false;
                state.CurrentState = EnemyState.EnemyStateType.Patrol;
                rb.linearVelocity = new Vector2(0f, 0f);
            }
        }
    } 
    public bool TakeDamage(int dmg, Vector2 attackerPos)
    {
        if (isInvincible)
            return false;

        hp -= dmg;

        isInvincible = true;
        iFrameTimer = iFrameTime;

        // Knockback
        if (knockbackable)
        {
            lastHitDirection = (transform.position - (Vector3)attackerPos).normalized;
            rb.AddForce(lastHitDirection * knockbackStrength, ForceMode2D.Impulse);

            knockbackTimer = knockbackTime;
            state.CurrentState = EnemyState.EnemyStateType.Hit;
            state.IsKnocked = true;
        }
        
        // Flash
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(HitFlash());


        // call death if hp <= 0
        if (hp <= 0)
        {
            Die();
            return true;
        }
        return false;
    }


    // death
    private void Die()
    {
        GetComponentInChildren<LumenDropper>()?.Drop();

        if (corpsePrefab != null)
        {
            GameObject corpse = Instantiate(
                corpsePrefab,
                transform.position,
                transform.rotation
            );

            corpse.GetComponent<Corpse>()?.Pop(lastHitDirection);
        }

        if (persistentObject != null)
        {
            switch (persistentObject.PersistenceType)
            {
                case PersistenceType.Temporary:
                    GameManager.Instance.activeRun.temporaryRemoved.Add(persistentObject.Id);
                    break;

                case PersistenceType.Permanent:
                    GameManager.Instance.activeRun.permanentRemoved.Add(persistentObject.Id);
                    break;
            }
        }

        // Switched off, not destroyed: a rest has to be able to put it back, and a
        // destroyed object cannot be put back without knowing how to rebuild it.
        gameObject.SetActive(false);
    }



    // white flash when hit
    private IEnumerator HitFlash()
    {
        float halfTime = hitFlashTime * 0.5f;
        float timer = 0f;

        // fade in
        while (timer < halfTime)
        {
            timer += Time.deltaTime;
            float t = timer / halfTime; // 0 -> 1
            mat.SetFloat("_FlashAmount", t);
            yield return null;
        }

        timer = 0f;

        // fade out
        while (timer < halfTime)
        {
            timer += Time.deltaTime;
            float t = 1f - (timer / halfTime); // 1 -> 0
            mat.SetFloat("_FlashAmount", t);
            yield return null;
        }

        mat.SetFloat("_FlashAmount", 0f);
    }
}