using UnityEngine;

// The only thing an attack needs to know about its target. Player, enemy and breakable
// wall all answer the same call, so no attacker ever asks what it just hit.
public interface IDamageable {
    bool TakeDamage(int damage, Vector2 attackerPos);
}