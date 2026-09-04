using UnityEngine;

// Every key the game listens to, read once a frame. Nothing else touches the input system,
// so rebinding or pausing is one change in one place.
public class PlayerInput : MonoBehaviour {
    public bool IsPaused => PauseManager.Instance != null && PauseManager.Instance.IsPaused; 
    
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

    private void Update() {
        // One gate, shared with Interactable and PauseManager. Keeping a private list
        // here is how the rest menu, the rest wave and the credits all ended up able
        // to flip the player and buffer a jump while he was meant to be frozen.
        if (UIState.Busy) {
            Movement = Vector2.zero;

            JumpPressed = false;
            JumpReleased = false;

            DashPressed = false;

            AttackPressed = false;
            AbilityPressed = false;
            HealAbilityHeld = false;

            HealPressed = false;
            InteractPressed = false;

            LookUpHeld = false;
            LookDownHeld = false;

            return;
        }

        ReadInput();
    }


    private void ReadInput() {
        Movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

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