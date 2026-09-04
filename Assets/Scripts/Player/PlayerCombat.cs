using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static PlayerHealth;
using static PlayerState;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    private Rigidbody2D rb;
    private PlayerInput input;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerState state;
    [SerializeField] private PlayerHealth health;
    [SerializeField] private Collider2D attackColliderRight;
    [SerializeField] private Collider2D attackColliderLeft;
    [SerializeField] private Collider2D attackColliderUp;
    [SerializeField] private Collider2D attackColliderDown;
    [SerializeField] private Transform attackPointRight;
    [SerializeField] private Transform attackPointLeft;
    [SerializeField] private Transform attackPointUp;
    [SerializeField] private Transform attackPointDown;
    [SerializeField] private GameObject slashPrefab;
    [SerializeField] private EffectSpawner effectSpawner;


    [Header("Settings")]
    private int damage = 2;
    [SerializeField] private int highDamage = 2;
    [SerializeField] private int midDamage = 3;
    [SerializeField] private int lowDamage = 5;
    [SerializeField] private int damagePerStrengthLevel = 1;
    [Space(2)]
    [SerializeField] private float attackCooldown = 0.2f;
    [SerializeField] private float attackDuration = 0.1f;
    [SerializeField] private float attackSpeed = 1f;

    // Same direction as the damage tiers above.
    [Header("Stability - hitbox size")]
    [SerializeField] private float highSize = 1f;
    [SerializeField] private float midSize = 1.1f;
    [SerializeField] private float lowSize = 1.25f;

    // Gap between swings only; attackDuration stays fixed.
    [Header("Stability - swing rate")]
    [SerializeField] private float highRate = 1f;
    [SerializeField] private float midRate = 1.1f;
    [SerializeField] private float lowRate = 1.2f;

    // The hitbox is live for 0.1s, too short to judge by eye.
    [SerializeField] private bool drawHitboxGizmo = true;
    [Space(2)]
    [SerializeField] private float recoilForceX = 5f;
    [SerializeField] private float recoilForceY = 2f;
    [SerializeField] private float pogoForce = 15f;
    [Space(2)]
    [SerializeField] private float hitPauseTime = 0.04f;
    [SerializeField] private float killPauseTime = 0.08f;
    
    [Header("Camera Shake")]
    [SerializeField] private float hitShakeDuration = 0.04f;
    [SerializeField] private float hitShakeAmplitude = 2f;
    [SerializeField] private float hitShakeFrequency = 2f;

    [SerializeField] private float killShakeDuration = 0.08f;
    [SerializeField] private float killShakeAmplitude = 3f;
    [SerializeField] private float killShakeFrequency = 2.5f;

    // Every swing, hit or miss. Below hitShakeAmplitude, so connecting still steps up.
    [Header("Swing Shake - by stability")]
    [SerializeField] private float midSwingDuration = 0.05f;
    [SerializeField] private float midSwingAmplitude = 0.8f;
    [SerializeField] private float midSwingFrequency = 2f;

    [SerializeField] private float lowSwingDuration = 0.07f;
    [SerializeField] private float lowSwingAmplitude = 1.5f;
    [SerializeField] private float lowSwingFrequency = 2.5f;

    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask breakableLayer;
    [SerializeField] private LayerMask spikeLayer;

    private float lastAttackTime;
    private ContactFilter2D filter;
    private ContactFilter2D gfilter;
    private Collider2D[] results = new Collider2D[10];


    private HashSet<IDamageable> hitDamageables = new();

    private AttackType currentAttackType;
    private int attackDir;

    // Absolute, never multiplied onto the current scale, so swings cannot compound.
    private Transform[] attackScalers;
    private Vector3[] attackBaseScales;
    private Vector3[] attackBasePositions;

    private float CooldownNow => attackCooldown / (attackSpeed * RateFor(Stability));

    private StabilityState Stability => health.CurrentStabilityState;

    public StabilityState CurrentStability => Stability;
    public float CurrentAttackScale => SizeFor(Stability);
    public float CurrentCooldown => CooldownNow;

    private void Awake()
    {
        // Disable Colliders on start
        attackColliderRight.enabled = false;
        attackColliderLeft.enabled = false;
        attackColliderUp.enabled = false;
        attackColliderDown.enabled = false;

        attackScalers = new[]
        {
            attackColliderRight.transform, attackColliderLeft.transform,
            attackColliderUp.transform,    attackColliderDown.transform
        };

        attackBaseScales = new Vector3[attackScalers.Length];
        attackBasePositions = new Vector3[attackScalers.Length];

        for (int i = 0; i < attackScalers.Length; i++)
        {
            attackBaseScales[i] = attackScalers[i].localScale;
            attackBasePositions[i] = attackScalers[i].localPosition;
        }

        filter = new ContactFilter2D();
        filter.SetLayerMask(enemyLayer | breakableLayer | spikeLayer);
        filter.useTriggers = true;

        gfilter = new ContactFilter2D();
        gfilter.SetLayerMask(groundLayer);
        gfilter.useTriggers = false;

        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<PlayerInput>();
    }

    private enum AttackType
    {
        Side,
        Up,
        Down
    }

    private void Update()
    {
        float cooldown = CooldownNow;

        if (input.AttackPressed && Time.time >= lastAttackTime + cooldown && !state.IsBusy)
        {
            lastAttackTime = Time.time;
            StartCoroutine(DoAttack());
        }
    }


    private IEnumerator DoAttack()
    {
        attackDir = state.IsFacingRight ? 1 : -1;

        // Not scaled by stability: the window to land a hit never shrinks.
        float active = attackDuration / attackSpeed;

        var (activeCollider, attackType) = GetAttackData();

        currentAttackType = attackType;

        state.IsAttacking = true;
        state.CurrentState = GetStateFromAttack(attackType);

        hitDamageables.Clear();

        switch (health.CurrentStabilityState)
        {
            case StabilityState.High:
                damage = highDamage;
                break;

            case StabilityState.Mid:
                damage = midDamage;
                break;

            case StabilityState.Low:
                damage = lowDamage;
                break;
        }

        if (GameManager.Instance != null)
            damage += GameManager.Instance.activeRun.strengthLevel * damagePerStrengthLevel;

        // Per swing, so healing, resting and loading all resolve with no wiring.
        ApplyAttackSize(SizeFor(Stability));

        // Before ProcessAttackHit, so a landed hit overrides it.
        ShakeSwing();

        SoundManager.Play(SoundId.Attack);

        SpawnSlash(attackType);
        activeCollider.enabled = true;

        ProcessAttackHit(activeCollider, attackType);

        yield return new WaitForSeconds(active);

        activeCollider.enabled = false;
        state.IsAttacking = false;
    }

    private float SizeFor(StabilityState s) =>
        s == StabilityState.High ? highSize : s == StabilityState.Mid ? midSize : lowSize;

    private float RateFor(StabilityState s) =>
        s == StabilityState.High ? highRate : s == StabilityState.Mid ? midRate : lowRate;

    private void ApplyAttackSize(float scale)
    {
        if (attackScalers == null) return;

        // Position scales too, so the arrangement grows outward instead of in place.
        // The offsets already point the right way, so one multiply covers all four.
        for (int i = 0; i < attackScalers.Length; i++)
        {
            if (attackScalers[i] == null) continue;

            attackScalers[i].localScale = attackBaseScales[i] * scale;
            attackScalers[i].localPosition = attackBasePositions[i] * scale;
        }

        // Overlap runs later this frame, and physics only refreshes shapes on its own step.
        Physics2D.SyncTransforms();
    }

    // Current tier, not the last swing's size. White High, yellow Mid, red Low.
    private void OnDrawGizmos()
    {
        if (!drawHitboxGizmo || health == null) return;

        StabilityState tier = Stability;
        float scale = SizeFor(tier);

        Gizmos.color = tier == StabilityState.High ? Color.white
                     : tier == StabilityState.Mid  ? Color.yellow
                     : Color.red;

        DrawHitbox(attackColliderRight, scale, 0);
        DrawHitbox(attackColliderLeft,  scale, 1);
        DrawHitbox(attackColliderUp,    scale, 2);
        DrawHitbox(attackColliderDown,  scale, 3);
    }

    private void DrawHitbox(Collider2D col, float scale, int index)
    {
        if (col == null) return;

        Transform t = col.transform;

        // Base values: the transform still holds the last swing's size and position.
        bool cached = attackBaseScales != null && index < attackBaseScales.Length;

        Vector3 baseScale = cached ? attackBaseScales[index] : t.localScale;
        Vector3 localPos = (cached ? attackBasePositions[index] : t.localPosition) * scale;

        Vector3 worldPos = t.parent != null ? t.parent.TransformPoint(localPos) : localPos;

        Gizmos.matrix = Matrix4x4.TRS(worldPos, t.rotation, baseScale * scale);

        if (col is PolygonCollider2D poly && poly.points.Length > 1)
        {
            Vector2[] p = poly.points;
            for (int i = 0; i < p.Length; i++)
                Gizmos.DrawLine(p[i] + poly.offset, p[(i + 1) % p.Length] + poly.offset);
        }
        else if (col is BoxCollider2D box)
        {
            Gizmos.DrawWireCube(box.offset, box.size);
        }

        Gizmos.matrix = Matrix4x4.identity;
    }

    // Process the attack
    private void ProcessAttackHit(Collider2D activeCollider, AttackType attackType)
    {
        bool recoilApplied = false;

        int count = activeCollider.Overlap(filter, results);

        for (int i = 0; i < count; i++)
        {
            if (results[i].transform.root == transform)
                continue;

            IDamageable damageable = results[i].GetComponentInParent<IDamageable>();

            if (damageable != null && !hitDamageables.Contains(damageable))
            {
                hitDamageables.Add(damageable);

                bool destroyed = damageable.TakeDamage( damage, transform.position  );

                SoundManager.Play(destroyed ? SoundId.Kill : SoundId.HitDealt);

                Transform hitTransform = ((MonoBehaviour)damageable).transform;

                effectSpawner.SpawnHitEffect( hitTransform.position, attackType != AttackType.Side  );

                if (!recoilApplied)
                {
                    TimeManager.Instance.HitStop(  destroyed ? killPauseTime : hitPauseTime );

                    ShakeHit(destroyed);

                    ApplyRecoil(attackType, damageable);

                    recoilApplied = true;
                }
            }
        }

        int groundHits = activeCollider.Overlap(gfilter, results);

        for (int i = 0; i < groundHits; i++)
        {
            if (results[i].transform.root == transform)
                continue;

            if (currentAttackType == AttackType.Down || currentAttackType == AttackType.Up)
                continue;

            if (!recoilApplied)
            {
                ApplyWallRecoil();
                recoilApplied = true;
            }
        }

}


    // getting data about which attack is happening
    private (Collider2D, AttackType) GetAttackData()
    {
        float vertical = movement.MoveInput.y;

        if (vertical > 0.5f)
            return (attackColliderUp, AttackType.Up);

        if (vertical < -0.5f && movement.LastOnGroundTime <= 0)
            return (attackColliderDown, AttackType.Down);

        return (attackDir == 1 ? attackColliderRight : attackColliderLeft, AttackType.Side);
    }

    // Fires on the swing, hit or miss.
    private void ShakeSwing()
    {
        if (CameraShakeManager.Instance == null) return;

        switch (Stability)
        {
            case StabilityState.Mid:
                CameraShakeManager.Instance.Shake(
                    midSwingDuration, midSwingAmplitude, midSwingFrequency);
                break;

            case StabilityState.Low:
                CameraShakeManager.Instance.Shake(
                    lowSwingDuration, lowSwingAmplitude, lowSwingFrequency);
                break;
        }
    }

    // shake screen
    private void ShakeHit(bool isKill)
    {
        if (CameraShakeManager.Instance == null) return;

        if (isKill)
        {
            CameraShakeManager.Instance.Shake(killShakeDuration, killShakeAmplitude, killShakeFrequency);
        }
        else
        {
            CameraShakeManager.Instance.Shake(hitShakeDuration, hitShakeAmplitude, hitShakeFrequency);
        }
    }

    // Recoil knockback / pogo when hitting
    private void ApplyRecoil(AttackType type, IDamageable hit = null)
    {
        int dir = attackDir;

        switch (type)
        {
            case AttackType.Side:
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                rb.AddForce(new Vector2(-dir * recoilForceX, 0), ForceMode2D.Impulse);
                break;

            // The target decides the launch, so pads can differ from enemies.
            case AttackType.Down:
                float force = pogoForce;
                if (hit is MonoBehaviour hitObject)
                {
                    PogoTarget pad = hitObject.GetComponentInParent<PogoTarget>();
                    if (pad != null) force *= pad.PogoMultiplier;
                }

                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
                break;

            case AttackType.Up:
                if (state.IsGrounded) break;

                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                rb.AddForce(Vector2.down * recoilForceY, ForceMode2D.Impulse);
                break;
        }
    }

    // Recoil from wall
    private void ApplyWallRecoil()
    {
        int dir = attackDir;
        
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        rb.AddForce(new Vector2(-dir * recoilForceX, 0), ForceMode2D.Impulse);
    }

    // Get state
    private PlayerStateType GetStateFromAttack(AttackType type)
    {
        switch (type)
        {
            case AttackType.Side: return PlayerStateType.SideAttack;
            case AttackType.Up: return PlayerStateType.UpAttack;
            case AttackType.Down: return PlayerStateType.DownAttack;
            default: return PlayerStateType.Idle;
        }
    }

    // Slash VFX
    private void SpawnSlash(AttackType type)
    {
        Transform point;
        Quaternion rot = Quaternion.identity;

        switch (type)
        {
            case AttackType.Side:
                if (attackDir == 1) // right
                {
                    point = attackPointRight;
                    rot = Quaternion.identity;
                }
                else // left
                {
                    point = attackPointLeft;
                    rot = Quaternion.Euler(0, 0, 180);
                }
                break;

            case AttackType.Up:
                point = attackPointUp;
                rot = Quaternion.Euler(0, 0, 90);
                break;

            case AttackType.Down:
                point = attackPointDown;
                rot = Quaternion.Euler(0, 0, -90);
                break;

            default:
                return;
        }

        GameObject slash = Instantiate(slashPrefab, transform);

        slash.transform.localPosition = point.localPosition;
        slash.transform.localRotation = rot;

        // Off the prefab's scale, not the instance's, so it cannot compound.
        slash.transform.localScale = slashPrefab.transform.localScale * SizeFor(Stability);

        Animator anim = slash.GetComponent<Animator>();

        anim.Play(Random.value < 0.5f ? "Slasher1" : "Slasher2");

    }
}