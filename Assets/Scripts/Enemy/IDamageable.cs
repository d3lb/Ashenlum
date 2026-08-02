using UnityEngine;

public interface IDamageable
{
    bool TakeDamage(int damage, Vector2 attackerPos);
}