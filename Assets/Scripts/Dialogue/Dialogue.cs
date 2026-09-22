using UnityEngine;

// An interactable that says something. NPCs, signs, notes, gravestones.
public class Dialogue : Interactable {
    [SerializeField] private Conversation conversation;

    [Header("After a boss")]
    // Blank for an NPC that never changes.
    [SerializeField] private string afterBossId;
    [SerializeField] private Conversation afterBossConversation;

    // Handed over when the after-boss conversation closes. Anything the shop sells fits here,
    // and each one raises its own notification from inside the run profile.
    [Header("Gift")]
    [SerializeField] private string giftId;
    [SerializeField] private ShopGood gift;

    // What he says once he has nothing left to hand over.
    [SerializeField] private Conversation afterGiftConversation;

    // Driven off the Interactable trigger, so he looks up exactly when the prompt appears.
    [Header("Looks up")]
    [SerializeField] private Animator animator;
    [SerializeField] private string nearBool = "PlayerNear";

    // Stays true one extra frame, so the closing press cannot reopen it.
    protected override bool CanInteract => !DialogueManager.IsDialogueActive;

    protected override string PromptVerb => "Talk";

    private bool BossBeaten {
        get {
            if (string.IsNullOrEmpty(afterBossId)) return false;

            var run = GameManager.Instance != null ? GameManager.Instance.activeRun : null;
            return run != null && run.defeatedBosses.Contains(afterBossId);
        }
    }

    private bool Gave =>
        !string.IsNullOrEmpty(giftId) && GameManager.Instance != null
        && GameManager.Instance.HasSeenEvent(giftId);

    // Only this line owes the player something, so only this line carries the callback.
    private bool OnGiftLine => BossBeaten && !Gave && afterBossConversation != null;

    // Read top down, each falling back to the one below, so a half-filled inspector still talks.
    private Conversation Current {
        get {
            if (Gave && afterGiftConversation != null) return afterGiftConversation;
            if (BossBeaten && afterBossConversation != null) return afterBossConversation;

            return conversation;
        }
    }

    protected override void Interact() {
        if (DialogueManager.Instance == null) return;
        DialogueManager.Instance.StartDialogue(Current, OnGiftLine ? Give : (System.Action)null);
    }

    private void Give() {
        if (gift == null || GameManager.Instance == null) return;

        if (string.IsNullOrEmpty(giftId)) {
            Debug.LogError($"[Dialogue] '{name}' has a gift but no Gift Id, so it cannot be saved.", this);
            return;
        }

        if (GameManager.Instance.HasSeenEvent(giftId)) return;

        // Recorded even at the cap: a gift he cannot hand over is still a gift he gave.
        GameRunProfile run = GameManager.Instance.activeRun;
        if (!gift.SoldOut(run)) gift.Purchase(run);

        GameManager.Instance.RegisterEvent(giftId);
        GameManager.Instance.MarkDirty();
    }

    protected override void OnPlayerEnter() {
        if (animator != null) animator.SetBool(nearBool, true);
    }

    protected override void OnPlayerExit() {
        if (animator != null) animator.SetBool(nearBool, false);
    }
}
