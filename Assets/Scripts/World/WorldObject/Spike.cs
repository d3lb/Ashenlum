using UnityEngine;

public class Spike : MonoBehaviour {
    [SerializeField] private int damageAmount = 9999;

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.CompareTag("Player")) {
            PlayerHealth playerHealth =
                collision.GetComponentInParent<PlayerHealth>();
            
            if (playerHealth != null) {
                playerHealth.TakeDamage(damageAmount, transform.position);
            }
        }
    }
}