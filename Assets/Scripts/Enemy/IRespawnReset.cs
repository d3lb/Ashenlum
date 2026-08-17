// Anything holding state a coroutine was supposed to clean up. Disabling a GameObject
// kills its coroutines outright - they never resume - so a component that turns a hitbox
// on before a yield and off after it will come back with that hitbox still on.
public interface IRespawnReset
{
    void ResetForRespawn();
}
