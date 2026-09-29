using UnityEngine;

// One dash clip drawn flat, pointed wherever he is going. The tilt sits on its own transform
// between the flip root and the art, so the Animator never fights it for the rotation.
public class SecretaryBirdAnimation : MonoBehaviour {
    [SerializeField] private Animator animator;
    [SerializeField] private Transform tiltRoot;

    // Two bools and nothing else. The wall-to-dash clip is a state the Animator passes through
    // on its own, not something anyone asks for.
    public void SetDashing(bool dashing) => animator.SetBool("IsDashing", dashing);
    public void SetPerched(bool perched) => animator.SetBool("OnWall", perched);

    // The coil he holds while hovering. The fall itself is the dash clip, tilted straight down.
    public void SetStomping(bool on) {
        animator.SetBool("IsStomping", on);

        if (on) ClearAim();
    }

    // Measured from the facing axis rather than from world right, because SetFacing mirrors
    // the whole art: the same positive angle reads as up-left once the parent is flipped.
    public void Aim(Vector2 dir) {
        if (tiltRoot == null || dir.sqrMagnitude < 0.0001f) return;

        float angle = Mathf.Atan2(dir.y, Mathf.Abs(dir.x)) * Mathf.Rad2Deg;
        tiltRoot.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void ClearAim() {
        if (tiltRoot != null) tiltRoot.localRotation = Quaternion.identity;
    }

    private void Awake() {
        if (animator == null)
            Debug.LogError($"[SecretaryBirdAnimation] '{name}' has no Animator.", this);

        if (tiltRoot == null)
            Debug.LogError($"[SecretaryBirdAnimation] '{name}' has no Tilt Root, so the dash " +
                           "will always point sideways.", this);
    }
}
