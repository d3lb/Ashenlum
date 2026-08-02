using UnityEngine;

public class BossTrigger : MonoBehaviour
{
    [SerializeField] private CameraSwitcher cameraSwitcher;
    [SerializeField] private SecretaryBirdBrain boss;
    private bool activated;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activated)
            return;

        if (!other.CompareTag("Player"))
            return;

        activated = true;
        boss.Activate();

        cameraSwitcher.SwitchToBossCam();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        activated = false;

        cameraSwitcher.SwitchToGameplayCam();
    }
}