using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerState;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    private Rigidbody2D rb;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerState states;
    [SerializeField] private Collider2D attackColliderRight;
    [SerializeField] private Collider2D attackColliderLeft;
    [SerializeField] private Collider2D attackColliderUp;
    [SerializeField] private Collider2D attackColliderDown;


    [Header("Settings")]
    [SerializeField] private int damage = 2;
    [SerializeField] private float attackCooldown = 0.2f;
    [SerializeField] private float attackDuration = 0.1f;
    [SerializeField] private float attackSpeed = 1f;
    [SerializeField] private float recoilForceX = 5f;
    [SerializeField] private float recoilForceY = 2f;
    [SerializeField] private float pogoForce = 15f;

    [Header("Layers")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask groundLayer;

    private float lastAttackTime;

    private ContactFilter2D filter;
    private Collider2D[] results = new Collider2D[10];
    private HashSet<EnemyHealth> hitEnemies = new HashSet<EnemyHealth>();
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

    private (Collider2D, AttackType) GetAttackData()
    {
        float vertical = movement.MoveInput.y;

        if (vertical > 0.5f)
            return (attackColliderUp, AttackType.Up);

        if (vertical < -0.5f && movement.LastOnGroundTime <= 0)
            return (attackColliderDown, AttackType.Down);

        return (states.IsFacingRight ? attackColliderRight : attackColliderLeft, AttackType.Side);
    }

    private IEnumerator DoAttack()
    {
        float active = attackDuration / attackSpeed;

        var (activeCollider, attackType) = GetAttackData();

        activeCollider.enabled = true;

        hitEnemies.Clear();

        float timer = 0f;

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
                    enemy.TakeDamage(damage, transform.position);

                    ApplyRecoil(attackType);
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }



        activeCollider.enabled = false;
    }


    private void ApplyRecoil(AttackType type)
    {
        int dir = states.IsFacingRight ? 1 : -1;

        switch (type)
        {
            case AttackType.Side:
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                rb.AddForce(new Vector2(-dir * recoilForceX, 0), ForceMode2D.Impulse);
                break;

            case AttackType.Down:
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
                rb.AddForce(Vector2.up * pogoForce, ForceMode2D.Impulse);
                break;

            case AttackType.Up:
                // later
                break;
        }
    }


}