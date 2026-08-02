using UnityEngine;

public class SecretaryBirdCollision : MonoBehaviour
{
    [SerializeField] private SecretaryBirdMovement movement;
    [SerializeField] private SecretaryBirdState state;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (state.CurrentState != SecretaryBirdState.BossStateType.Dash &&
            state.CurrentState != SecretaryBirdState.BossStateType.Dive)
        {
            return;
        }

        movement.FinishMovement();
    }
}