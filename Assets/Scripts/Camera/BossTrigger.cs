using UnityEngine;

// Place this box wherever the fight should start, not necessarily the entrance.
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
