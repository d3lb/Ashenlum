using UnityEngine;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private PlayerState states;

    [SerializeField] private float attackCooldown = 0.4f;
    [SerializeField] private float attackDuration = 0.1f;

    [SerializeField] private Collider2D attackCollider;

    private float lastAttackTime;

    private void Awake()
    {
        attackCollider.enabled = false;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            StartCoroutine(DoAttack());
        }
    }

    private IEnumerator DoAttack()
    {
        attackCollider.enabled = true;

        yield return new WaitForSeconds(attackDuration);

        attackCollider.enabled = false;
    }
}