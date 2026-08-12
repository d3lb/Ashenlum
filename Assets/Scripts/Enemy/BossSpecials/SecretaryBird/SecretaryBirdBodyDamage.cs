using UnityEngine;

/// <summary>
/// Turns the boss's body contact damage off while he is repositioning, and on the rest
/// of the time.
///
/// A reposition blink is not an attack: it gets a short flash, a safe-coloured line and no
/// dash hitbox, but the body hitbox was still live - so crossing the arena hurt anyway, with
/// far less warning than any real attack gives. That is the part that read as luck rather
/// than skill. Everything else he does stays dangerous to touch.
/// </summary>
public class SecretaryBirdBodyDamage : MonoBehaviour
{
    [SerializeField] private SecretaryBirdState state;

    [Tooltip("The EnemyHitbox on the boss body itself - NOT the dash or dive children.")]
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
