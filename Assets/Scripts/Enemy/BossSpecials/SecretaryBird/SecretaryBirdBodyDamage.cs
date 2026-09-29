using UnityEngine;

public class SecretaryBirdBodyDamage : MonoBehaviour {
    [SerializeField] private SecretaryBirdState state;
    [SerializeField] private SecretaryBirdMovement move;

    [SerializeField] private EnemyHitbox bodyHitbox;

    private void Awake() {
        if (state == null)      state      = GetComponent<SecretaryBirdState>();
        if (move == null)       move       = GetComponent<SecretaryBirdMovement>();
        if (bodyHitbox == null) bodyHitbox = GetComponent<EnemyHitbox>();
    }

    private void Update() {
        if (state == null || bodyHitbox == null) return;

        // The dash box is the damage while he is flying, so the body must not double up on it
        // or catch the player from behind.
        bool dashing = move != null && move.IsDashing;

        bool allowed = !dashing
                       && state.CurrentState != SecretaryBirdState.BossStateType.Reposition;

        if (bodyHitbox.enabled != allowed)
            bodyHitbox.enabled = allowed;
    }
}
