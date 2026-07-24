using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public Vector2 Movement { get; private set; }

    public bool JumpPressed { get; private set; }
    public bool JumpReleased { get; private set; }

    public bool DashPressed { get; private set; }

    public bool AttackPressed { get; private set; }
    public bool AbilityPressed { get; private set; }
    public bool HealAbilityHeld { get; private set; }

    public bool HealPressed { get; private set; }
    public bool InteractPressed { get; private set; }

    public bool LookUpHeld { get; private set; }
    public bool LookDownHeld { get; private set; }

    private void Update()
    {
        Movement = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        JumpPressed = Input.GetKeyDown(KeyCode.Space);
        JumpReleased = Input.GetKeyUp(KeyCode.Space);

        DashPressed = Input.GetKeyDown(KeyCode.LeftShift);

        AttackPressed = Input.GetMouseButtonDown(0);
        AbilityPressed = Input.GetMouseButtonDown(1);
        HealAbilityHeld = Input.GetKey(KeyCode.F);

        HealPressed = Input.GetKeyDown(KeyCode.R);
        InteractPressed = Input.GetKeyDown(KeyCode.E);

        LookUpHeld = Input.GetKey(KeyCode.W);
        LookDownHeld = Input.GetKey(KeyCode.S);
    }
}