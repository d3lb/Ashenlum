using UnityEngine;

// Death leaves a scatter of particles where the body used to be. Still a Corpse: WorldReset
// sweeps this type on a rest, so the remains clear exactly when everything else does.
public class Corpse : MonoBehaviour {
    [SerializeField] private ParticleSystem particles;

    // Degrees the spray leans away from whatever landed the killing blow.
    [SerializeField] private float tilt = 25f;

    // Paused rather than left to expire: a paused system holds its particles forever, so the
    // lifetime does not have to be set to some absurd number to make them stay put.
    [SerializeField] private float freezeAfter = 2.5f;

    private float freezeAt;
    private bool frozen;

    private void Awake() {
        if (particles == null) particles = GetComponentInChildren<ParticleSystem>();

        if (particles == null)
            Debug.LogError($"[Corpse] '{name}' has no Particle System, so death leaves nothing.", this);

        freezeAt = Time.time + freezeAfter;
    }

    public void Pop(Vector2 direction) {
        float sign = direction.x >= 0f ? 1f : -1f;

        // World space and on the root only, so the child keeps whatever rotation aims its cone.
        transform.Rotate(0f, 0f, -sign * tilt, Space.World);

        if (particles != null) particles.Play();
    }

    private void Update() {
        if (frozen || Time.time < freezeAt) return;

        frozen = true;
        if (particles != null) particles.Pause();
    }
}
