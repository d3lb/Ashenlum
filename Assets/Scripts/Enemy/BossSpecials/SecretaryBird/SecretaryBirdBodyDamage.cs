using UnityEngine;

public class SecretaryBirdBodyDamage : MonoBehaviour
{
    [SerializeField] private SecretaryBirdState state;

    [SerializeField] private EnemyHitbox bodyHitbox;

    private void Awake()
    {
        if (state == null)      state      = GetComponent<SecretaryBirdState>();
        if (bodyHitbox == null) bodyHitbox = GetComponent<EnemyHitbox>();
    }

    private void Update()
    {
        if (state == null || bodyHitbox == null) return;

        bool allowed = state.CurrentState != SecretaryBirdState.BossStateType.Reposition;

        if (bodyHitbox.enabled != allowed)
            bodyHitbox.enabled = allowed;
    }
}
