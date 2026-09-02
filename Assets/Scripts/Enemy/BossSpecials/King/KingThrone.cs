using UnityEngine;

// "E - Challenge". Put this on its own child object, never on the King himself:
// Interactable forces isTrigger on this object's collider, which would turn his
// damage collider into a trigger and quietly break the fight.
public class KingThrone : Interactable
{
    [SerializeField] private KingEncounter encounter;

    [SerializeField] private string verb = "Challenge";

    // Hides the prompt once the fight is running or already won, without needing
    // anything to remember to switch this object off.
    protected override bool CanInteract =>
        encounter != null && !encounter.Started && !encounter.AlreadyBeaten;

    protected override string PromptVerb => verb;

    protected override void Awake()
    {
        base.Awake();

        if (encounter == null)
            Debug.LogError($"[KingThrone] '{name}' has no Encounter assigned.", this);
    }

    protected override void Interact()
    {
        encounter.Begin();
    }
}
