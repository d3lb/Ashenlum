using UnityEngine;

// Trip-wire, nothing more. Put this box wherever you want the fight to actually start -
// it does not have to be the room entrance.
[RequireComponent(typeof(Collider2D))]
public class BossTrigger : MonoBehaviour
{
    [SerializeField] private BossEncounter encounter;

    private bool fired;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (fired) return;
        if (!other.CompareTag("Player")) return;
        if (encounter == null) return;

        fired = true;
        encounter.Begin();
    }
}
