using UnityEngine;

public class PlayerAnimation : MonoBehaviour {
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerState state;

    private void Update() {
        animator.SetInteger("State", (int)state.CurrentState);
    }
}