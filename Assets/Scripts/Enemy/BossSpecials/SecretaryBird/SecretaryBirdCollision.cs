using UnityEngine;

/// <summary>
/// Forwards physical impacts to the movement component. The player is deliberately
/// filtered out by the layer mask on SecretaryBirdMovement, so a dash passes THROUGH
/// the player and only ever ends on geometry.
/// </summary>
public class SecretaryBirdCollision : MonoBehaviour
{
    [SerializeField] private SecretaryBirdMovement movement;

    private void Reset() => movement = GetComponent<SecretaryBirdMovement>();

    private void OnCollisionEnter2D(Collision2D collision) => movement.ReportImpact(collision);
    private void OnCollisionStay2D(Collision2D collision)  => movement.ReportImpact(collision);
}
