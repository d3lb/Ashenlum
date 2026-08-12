// Every unlockable traversal/combat ability in the game.
// Dash, DoubleJump and WallJump are mirrored onto the legacy bools in
// GameRunProfile because PlayerMovement reads those directly — see
// InventoryManager.UnlockAbility.
public enum AbilityType
{
    Dash,
    DoubleJump,
    WallJump,
    WingBurst
}
