using UnityEngine;

/// <summary>
/// Owns the hitbox GameObjects. Each hitbox object should carry your existing
/// EnemyHitbox component - this class only turns them on and off.
/// </summary>
public class SecretaryBirdAttackController : MonoBehaviour
{
    [SerializeField] private SecretaryBirdState state;

    [Header("Dash")]
    [SerializeField] private GameObject dashHitboxLeft;
    [SerializeField] private GameObject dashHitboxRight;

    [Header("Dive / Slam")]
    [SerializeField] private GameObject diveHitbox;

    private void Awake() => DisableAllHitboxes();

    public void EnableDashHitbox()
    {
        Set(state.IsFacingRight ? dashHitboxRight : dashHitboxLeft, true);
        Set(state.IsFacingRight ? dashHitboxLeft  : dashHitboxRight, false);
    }

    public void DisableDashHitbox()
    {
        Set(dashHitboxLeft, false);
        Set(dashHitboxRight, false);
    }

    public void EnableDiveHitbox()  => Set(diveHitbox, true);
    public void DisableDiveHitbox() => Set(diveHitbox, false);

    public void DisableAllHitboxes()
    {
        DisableDashHitbox();
        DisableDiveHitbox();
    }

    private static void Set(GameObject go, bool on)
    {
        if (go != null && go.activeSelf != on) go.SetActive(on);
    }
}
