using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private string currentAnimation = "";

    private PlayerMovement playerMovement;


    public void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void ChangeAnimation(string animation, float crossFade = 0.2f)
    {
        if (currentAnimation != animation)
        {
            currentAnimation = animation;
            animator.CrossFade(animation, crossFade);
        }
    }

    public void CheckAnimation()
    {
        if (playerMovement.RB.linearVelocity.x != 0)
        {
            ChangeAnimation("Run");
        }
        else
        {
            ChangeAnimation("Idle");
        }
    }
}
