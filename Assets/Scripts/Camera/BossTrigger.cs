using UnityEngine;

public class BossTrigger : MonoBehaviour
{
    [SerializeField] private CameraSwitcher cameraSwitcher;

    private bool activated;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activated)
            return;

        if (!other.CompareTag("Player"))
            return;

        activated = true;

        cameraSwitcher.SwitchToBossCam();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (activated)
            return;

        if (!other.CompareTag("Player"))
            return;

        activated = false;

        cameraSwitcher.SwitchToGameplayCam();
    }
}