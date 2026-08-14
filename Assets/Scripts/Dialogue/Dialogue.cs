using UnityEngine;

// An interactable that says something. NPCs, signs, notes, gravestones.
public class Dialogue : Interactable
{
    [SerializeField] private Conversation conversation;

    // The manager holds IsDialogueActive true for one extra frame after closing, which
    // outlasts PlayerInput's latched keypress - so the press that ends a conversation
    // cannot immediately restart it.
    protected override bool CanInteract => !DialogueManager.IsDialogueActive;

    protected override string PromptVerb => "Talk";

    protected override void Interact()
    {
        DialogueManager.Instance.StartDialogue(conversation);
    }
}
