using UnityEngine;

// An interactable that says something. NPCs, signs, notes, gravestones.
public class Dialogue : Interactable
{
    [SerializeField] private Conversation conversation;

    [Header("After a boss")]
    // Leave the id blank for an NPC that always says the same thing.
    [SerializeField] private string afterBossId;
    [SerializeField] private Conversation afterBossConversation;

    // IsDialogueActive stays true one extra frame so the closing press cannot reopen it.
    protected override bool CanInteract => !DialogueManager.IsDialogueActive;

    protected override string PromptVerb => "Talk";

    private bool BossBeaten
    {
        get
        {
            if (string.IsNullOrEmpty(afterBossId)) return false;

            var run = GameManager.Instance != null ? GameManager.Instance.activeRun : null;
            return run != null && run.defeatedBosses.Contains(afterBossId);
        }
    }

    // Falls back to the normal line, so a half-filled inspector still talks.
    private Conversation Current =>
        BossBeaten && afterBossConversation != null ? afterBossConversation : conversation;

    protected override void Interact()
    {
        if (DialogueManager.Instance == null) return;
        DialogueManager.Instance.StartDialogue(Current);
    }
}
