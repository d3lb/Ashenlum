using UnityEngine;

// An interactable that says something. NPCs, signs, notes, gravestones.
public class Dialogue : Interactable
{
    [SerializeField] private Conversation conversation;

    // IsDialogueActive stays true one extra frame so the closing press cannot reopen it.
    protected override bool CanInteract => !DialogueManager.IsDialogueActive;

    protected override string PromptVerb => "Talk";

    protected override void Interact()
    {
        if (DialogueManager.Instance == null) return;
        DialogueManager.Instance.StartDialogue(conversation);
    }
}
