using UnityEngine;
using UnityEngine.UI;

// One dash charge. Goes on the pip root, so the background hides with it.
public class DashPipUI : MonoBehaviour {
    [SerializeField] private Image front;

    // Needs Front set to Filled; a Simple image drops fillAmount silently.
    [SerializeField] private bool animateRefill;

    private bool CanAnimate =>
        animateRefill && front != null && front.type == Image.Type.Filled;

    private void Awake() {
        if (animateRefill && front != null && front.type != Image.Type.Filled)
            Debug.LogError($"[DashPipUI] '{name}' has Animate Refill ticked but " +
                           $"'{front.name}' is Image Type {front.type}, not Filled. " +
                           "Falling back to showing and hiding the pip.", this);
    }

    public void Set(bool held, float refillPercent) {
        if (front == null) return;

        if (held) {
            front.enabled = true;
            if (CanAnimate) front.fillAmount = 1f;
            return;
        }

        if (CanAnimate && refillPercent > 0f) {
            front.enabled = true;
            front.fillAmount = refillPercent;
            return;
        }

        front.enabled = false;
    }
}
