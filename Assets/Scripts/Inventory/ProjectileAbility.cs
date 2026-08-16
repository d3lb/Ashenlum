using UnityEngine;

[CreateAssetMenu(fileName = "New Projectile Ability", menuName = "Ashenlum/Ability/Projectile")]
public class ProjectileAbility : ActiveAbility
{
    [Header("Shot")]
    [SerializeField] private AbilityProjectile projectilePrefab;
    [SerializeField] private float speed = 20f;
    [SerializeField] private int damage = 3;
    [SerializeField] private Vector2 spawnOffset = new Vector2(0.6f, 0f);

    public override void Fire(PlayerActiveAbility user)
    {
        if (projectilePrefab == null) return;

        Vector2 dir = user.AimDirection;
        Vector3 pos = user.transform.position
                    + new Vector3(spawnOffset.x * Mathf.Sign(dir.x), spawnOffset.y, 0f);

        AbilityProjectile shot = Instantiate(projectilePrefab, pos, Quaternion.identity);
        shot.Launch(dir, speed, damage);
    }
}
