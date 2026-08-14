using UnityEngine;

public class NPCInteractable : Interactable
{
    [SerializeField] private Dialogue dialogue;

    protected override bool CanInteract => !DialogueManager.IsDialogueActive;

    protected override string PromptVerb => "Talk";

    protected override void Interact()
    {
        DialogueManager.Instance.StartDialogue(dialogue);
    }
}
