// Every unlockable traversal/combat ability in the game.
// Unlock state lives on GameRunProfile — see IsAbilityUnlocked / SetAbilityUnlocked.
// This enum only exists so UI can ask about an ability without hard-coding a field name.
public enum AbilityType
{
    Dash,
    DoubleJump,
    WallJump,
    WingBurst
}
