// Coroutines die with the object; this undoes what they never finished.
// Called with the object already active, so starting a coroutine here is allowed.
public interface IRespawnReset
{
    void ResetForRespawn();
}
