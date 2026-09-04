using UnityEngine;

// Own child object, never the King: Interactable forces isTrigger on this collider.
public class KingThrone : Interactable
{
    [SerializeField] private KingEncounter encounter;

    [SerializeField] private string verb = "Challenge";

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
