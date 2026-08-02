using System.Collections;
using UnityEngine;

public class SecretaryBirdBrain : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SecretaryBirdState state;
    [SerializeField] private SecretaryBirdMovement movement;

    [Header("Detection")]
    [SerializeField] private Transform player;
    [SerializeField] private float meleeRange = 3f;

    private Coroutine attackRoutine;
    private bool active;
    private void Update()
    {

        if (!active)
            return;

        if (state.IsDead)
            return;

        if (state.IsBusy)
            return;

        if (attackRoutine != null)
            return;

        attackRoutine = StartCoroutine(ChooseAttack());
    }

    public void Activate()
    {
        active = true;
        state.CurrentState = SecretaryBirdState.BossStateType.Idle;
    }
    private IEnumerator ChooseAttack()
    {
        state.CurrentState = SecretaryBirdState.BossStateType.ChoosingAttack;

        yield return null;

        float distance =
            Vector2.Distance(
                transform.position,
                player.position
            );

        if (distance <= meleeRange)
        {
            attackRoutine =
                StartCoroutine(
                    movement.FlyDiveAttack(player)
                );
        }
        else
        {
            attackRoutine =
                StartCoroutine(
                    movement.DashAttack(player)
                );
        }

        yield return attackRoutine;

        attackRoutine = null;
    }
}