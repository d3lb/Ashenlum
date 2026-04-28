using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerState;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    private Rigidbody2D rb;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerState state;
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


    [Header("Settings")]
    [SerializeField] private int damage = 2;
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
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask groundLayer;

    private float lastAttackTime;
    private ContactFilter2D filter;
    private ContactFilter2D gfilter;
    private Collider2D[] results = new Collider2D[10];
    private HashSet<EnemyHealth> hitEnemies = new HashSet<EnemyHealth>();

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
        filter.SetLayerMask(enemyLayer);
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

        if (Input.GetMouseButtonDown(0) && Time.time >= lastAttackTime + cooldown)
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
        SpawnSlash(attackType);

        state.IsAttacking = true;
        state.CurrentState = GetStateFromAttack(attackType);
        activeCollider.enabled = true;

        hitEnemies.Clear();

        float timer = 0f;
        bool recoilApplied = false;

        while (timer < active)
        {
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

                    if (!recoilApplied)
                    {
                        StartCoroutine(HitPause(isDead ? killPauseTime : hitPauseTime));
                        ShakeHit(isDead);
                        ApplyRecoil(attackType);
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


            timer += Time.deltaTime;
            yield return null;
        }

        activeCollider.enabled = false;
        state.IsAttacking = false;
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
            StartCoroutine(cameraShake.Shake(
                killShakeDuration,
                killShakeAmplitude,
                killShakeFrequency
            ));
        }
        else
        {
            StartCoroutine(cameraShake.Shake(
                hitShakeDuration,
                hitShakeAmplitude,
                hitShakeFrequency
            ));
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
                    rot = Quaternion.Euler(0, 0, -90);
                }
                else // left
                {
                    point = attackPointLeft;
                    rot = Quaternion.Euler(0, 0, 90);
                }
                break;

            case AttackType.Up:
                point = attackPointUp;
                rot = Quaternion.identity;
                break;

            case AttackType.Down:
                point = attackPointDown;
                rot = Quaternion.Euler(0, 0, 180);
                break;

            default:
                return;
        }

        GameObject slash = Instantiate(slashPrefab, transform);
        slash.transform.localPosition = point.localPosition;
        slash.transform.localRotation = rot;
    }


    // hit pause effect
    private IEnumerator HitPause(float duration)
    {
        float originalTimeScale = Time.timeScale;

        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = originalTimeScale;
    }
}