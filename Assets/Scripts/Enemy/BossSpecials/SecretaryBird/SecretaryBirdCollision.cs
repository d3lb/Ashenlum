using UnityEngine;

public class SecretaryBirdCollision : MonoBehaviour
{
    [SerializeField] private SecretaryBirdMovement movement;

    private void Reset() => movement = GetComponent<SecretaryBirdMovement>();

    private void OnCollisionEnter2D(Collision2D collision) => movement.ReportImpact(collision);
    private void OnCollisionStay2D(Collision2D collision)  => movement.ReportImpact(collision);
}
