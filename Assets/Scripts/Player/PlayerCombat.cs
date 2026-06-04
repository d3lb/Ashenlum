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
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerState state;
    [SerializeField] private PlayerHealth health;
    [SerializeField] private CameraShake cameraShake;
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

    [SerializeField] private float attackCooldown = 0.2f;
    [SerializeField] private float attackDuration = 0.1f;
    [SerializeField] private float attackSpeed = 1f;
    [SerializeField] private float recoilForceX = 5f;
    [SerializeField] private float recoilForceY = 2f;
    [SerializeField] private float pogoForce = 15f;
    [SerializeField] private float hitPauseTime = 0.04f;
    [SerializeField] private float killPauseTime = 0.08f;

    [Header("Camera Shake")]
    [SerializeField] private float hitShakeDuration = 0.04f;
    [SerializeField] private float hitShakeAmplitude = 2f;
    [SerializeField] private float hitShakeFrequency = 2f;

    [SerializeField] private float killShakeDuration = 0.08f;
    [SerializeField] private float killShakeAmplitude = 3f;
    [SerializeField] private float killShakeFrequency = 2.5f;

    [Header("Layers")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask breakableLayer;
    [SerializeField] private LayerMask spikeLayer;

    private float lastAttackTime;
    private ContactFilter2D filter;
    private ContactFilter2D gfilter;
    private Collider2D[] results = new Collider2D[10];
    private HashSet<EnemyHealth> hitEnemies = new HashSet<EnemyHealth>();
    private HashSet<BreakableWall> hitWalls = new HashSet<BreakableWall>();
    private HashSet<Spike> hitSpikes = new HashSet<Spike>();


    private AttackType currentAttackType;
    private int attackDir;

    private void Awake()
    {
        // Disable Colliders on start
        attackColliderRight.enabled = false;
        attackColliderLeft.enabled = false;
        attackColliderUp.enabled = false;
        attackColliderDown.enabled = false;

        filter = new ContactFilter2D();
        filter.SetLayerMask(enemyLayer | breakableLayer | spikeLayer);
        filter.useTriggers = true;

        gfilter = new ContactFilter2D();
        gfilter.SetLayerMask(groundLayer);
        gfilter.useTriggers = false;

        rb = GetComponent<Rigidbody2D>();
    }

    private enum AttackType
    {
        Side,
        Up,
        Down
    }

    private void Update()
    {


        float cooldown = attackCooldown / attackSpeed;

        if (Input.GetMouseButtonDown(0) && Time.time >= lastAttackTime + cooldown && !state.IsBusy)
        {
            lastAttackTime = Time.time;
            StartCoroutine(DoAttack());
        }
    }


    private IEnumerator DoAttack()
    {
        attackDir = state.IsFacingRight ? 1 : -1;

        float active = attackDuration / attackSpeed;

        var (activeCollider, attackType) = GetAttackData();

        currentAttackType = attackType;

        state.IsAttacking = true;
        state.CurrentState = GetStateFromAttack(attackType);

        hitEnemies.Clear();
        hitWalls.Clear();
        hitSpikes.Clear();

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

        SpawnSlash(attackType);
        activeCollider.enabled = true;

        ProcessAttackHit(activeCollider, attackType);

        yield return new WaitForSeconds(active);

        activeCollider.enabled = false;
        state.IsAttacking = false;
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

            EnemyHealth enemy = results[i].attachedRigidbody?.GetComponent<EnemyHealth>();

            if (enemy != null && !hitEnemies.Contains(enemy))
            {
                hitEnemies.Add(enemy);
                bool isDead = enemy.TakeDamage(damage, transform.position);

                effectSpawner.SpawnHitEffect(enemy.transform.position, attackType != AttackType.Side);

                if (!recoilApplied)
                {
                    TimeManager.Instance.HitStop(isDead ? killPauseTime : hitPauseTime);
                    ShakeHit(isDead);
                    ApplyRecoil(attackType);
                    recoilApplied = true;
                }
            }
            BreakableWall wall = results[i].GetComponent<BreakableWall>();

            if (wall != null && !hitWalls.Contains(wall))
            {
                hitWalls.Add(wall);
                bool isBroken = wall.TakeDamage();

                if (!recoilApplied)
                {
                    TimeManager.Instance.HitStop(isBroken ? killPauseTime : hitPauseTime);
                    ShakeHit(isBroken);
                }
            }

            Spike spike = results[i].GetComponentInParent<Spike>();

            if (spike != null)
            {
                if (attackType == AttackType.Down)
                {
                    rb.linearVelocity =
                        new Vector2(
                            rb.linearVelocity.x,
                            pogoForce
                        );

                    ApplyRecoil(attackType);
                }
            }

            PogoTarget pogoTarget = results[i].GetComponentInParent<PogoTarget>();

            if (pogoTarget != null)
            {
                if (attackType == AttackType.Down)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, pogoForce*1.5f);
                    ApplyRecoil(attackType);
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

    // shake screen
    private void ShakeHit(bool isKill)
    {
        if (isKill)
        {
            CameraShake.Instance.Shake(killShakeDuration, killShakeAmplitude, killShakeFrequency);
        }
        else
        {
            CameraShake.Instance.Shake(hitShakeDuration, hitShakeAmplitude, hitShakeFrequency);
        }
    }

    // Recoil knockback / pogo when hitting
    private void ApplyRecoil(AttackType type)
    {
        int dir = attackDir;

        switch (type)
        {
            case AttackType.Side:
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                rb.AddForce(new Vector2(-dir * recoilForceX, 0), ForceMode2D.Impulse);
                break;

            // pogo
            case AttackType.Down:
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                rb.AddForce(Vector2.up * pogoForce, ForceMode2D.Impulse);
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

        Animator anim = slash.GetComponent<Animator>();

        anim.Play(Random.value < 0.5f ? "Slasher1" : "Slasher2");

    }
}