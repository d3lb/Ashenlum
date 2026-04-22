using UnityEngine;

public class PlayerAttackHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 2;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("TRIGGER WORKING with: " + other.name);

        if (other.TryGetComponent<EnemyHealth>(out var enemy))
        {
            enemy.TakeDamage(damage, transform.position);
        }
    }
}