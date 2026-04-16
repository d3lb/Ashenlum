using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private PlayerState states;

    [SerializeField] private float attackCooldown = 0.4f;
    [SerializeField] private float attackRange = 2f;

    private float lastAttackTime;

    [Header("Checks")]
    [SerializeField] private Transform _sideAttackPoint;
    [SerializeField] private Vector2 _sideAttackPointSize = new Vector2(2f, 2f);

    [Space(4)]
    [SerializeField] private LayerMask enemyLayer;

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            Attack();
        }
    }

    private void Attack()
    {


        int dir = states.IsFacingRight ? 1 : -1;
        Vector2 pos = (Vector2)transform.position + Vector2.right * dir * attackRange;
        Collider2D[] hits = Physics2D.OverlapBoxAll(pos, _sideAttackPointSize, 0, enemyLayer);


        foreach (var hit in hits)
        {
            Debug.Log("Hit: " + hit.name);
        }
    }


    #region EDITOR METHODS
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        int dir =  states.IsFacingRight ? 1 : -1;
        Vector2 pos = (Vector2)transform.position + Vector2.right * dir * attackRange;

        Gizmos.DrawWireCube(pos, _sideAttackPointSize);
    }
    #endregion
}
