using UnityEngine;

public class SecretaryBirdAttackController : MonoBehaviour {
    // One box, parented under the tilt root. It inherits the flip and the dash angle from the
    // art, so it always points where he is actually flying - no left and right pair.
    [Header("Dash")]
    [SerializeField] private GameObject dashHitbox;

    [Header("Dive / Slam")]
    [SerializeField] private GameObject diveHitbox;

    private void Awake() => DisableAllHitboxes();

    public void EnableDashHitbox()  => Set(dashHitbox, true);
    public void DisableDashHitbox() => Set(dashHitbox, false);

    public void EnableDiveHitbox()  => Set(diveHitbox, true);
    public void DisableDiveHitbox() => Set(diveHitbox, false);

    public void DisableAllHitboxes() {
        DisableDashHitbox();
        DisableDiveHitbox();
    }

    private static void Set(GameObject go, bool on) {
        if (go != null && go.activeSelf != on) go.SetActive(on);
    }
}
