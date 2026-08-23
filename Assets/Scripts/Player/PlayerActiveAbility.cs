using System.Collections;
using UnityEngine;

public class PlayerActiveAbility : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerState state;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Animator animator;

    [Header("Timing")]
    [SerializeField] private float postHitLockTime = 0.5f;
    [SerializeField] private float recoverTime = 0.2f;
    // How long it takes to settle to a dead stop, same as the burst heal.
    [SerializeField] private float slowTime = 0.5f;

    private PlayerInput input;
    private Rigidbody2D rb;

    private bool casting;
    private float chargeTimer;
    private float readyAt;
    private float cooldownStart;
    private float cooldownLength;

    public Vector2 AimDirection => state.IsFacingRight ? Vector2.right : Vector2.left;

    public ActiveAbility Equipped =>
        GameManager.Instance != null ? GameManager.Instance.activeRun.equippedAbility : null;

    public float ChargePercent
    {
        get
        {
            ActiveAbility ability = Equipped;
            if (ability == null || ability.chargeTime <= 0f) return 0f;
            return Mathf.Clamp01(chargeTimer / ability.chargeTime);
        }
    }

    public bool IsCharging => casting;

    public float CooldownPercent =>
        cooldownLength <= 0f ? 1f : Mathf.Clamp01((Time.time - cooldownStart) / cooldownLength);

    private void Awake()
    {
        input = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (input.AbilityPressed) TryCast();
    }

    private void TryCast()
    {
        ActiveAbility ability = Equipped;
        if (ability == null) return;

        if (casting) return;
        if (Time.time < readyAt) return;
        if (Time.time < playerHealth.LastHitTime + postHitLockTime) return;

        // The F burst owns this flag too - whichever starts first keeps the player.
        if (state.IsUsingAbility) return;

        StartCoroutine(Cast(ability));
    }

    private IEnumerator Cast(ActiveAbility ability)
    {
        casting = true;
        chargeTimer = 0f;
        state.IsUsingAbility = true;

        if (animator != null) animator.SetBool("AbilityCharging", true);

        Vector2 startVelocity = rb.linearVelocity;

        while (chargeTimer < ability.chargeTime)
        {
            if (Time.time <= playerHealth.LastHitTime + 0.01f)
            {
                EndCast();
                yield break;
            }

            chargeTimer += Time.deltaTime;

            // Pinned every frame, so gravity cannot pull the cast out of the air.
            float t = slowTime <= 0f ? 1f : Mathf.Clamp01(chargeTimer / slowTime);
            rb.linearVelocity = Vector2.Lerp(startVelocity * 0.5f, Vector2.zero, t);

            yield return null;
        }

        ability.Fire(this);

        readyAt = Time.time + ability.cooldown;
        cooldownStart = Time.time;
        cooldownLength = ability.cooldown;

        if (animator != null) animator.SetTrigger("AbilityRelease");

        yield return new WaitForSeconds(recoverTime);

        EndCast();
    }

    private void EndCast()
    {
        casting = false;
        chargeTimer = 0f;
        state.IsUsingAbility = false;

        if (animator != null) animator.SetBool("AbilityCharging", false);
    }
}
