using UnityEngine;

public class BreakablePlatformTrigger : MonoBehaviour {
    [SerializeField] private BreakablePlatform platform;

    private void OnTriggerStay2D(Collider2D other) {
        if (!other.CompareTag("Player")) return;
        platform.TriggerBreak();
    }
}