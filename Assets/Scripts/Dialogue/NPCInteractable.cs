using UnityEngine;

// Drop this on any NPC (or sign, shrine, note…) that should talk when the player
// stands next to it and presses E. Mirrors the CheckPoint interaction pattern:
// cache the player's PlayerInput on trigger enter, act on InteractPressed. The
// NPC owns only its own Dialogue asset — the DialogueManager owns the UI.
[RequireComponent(typeof(Collider2D))]
public class NPCInteractable : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private Dialogue dialogue;

    [Header("Optional")]
    [Tooltip("A world-space 'Press E' icon, shown while the player is in range. Optional.")]
    [SerializeField] private GameObject interactPrompt;

    private PlayerInput input;
    private bool        isPlayerInRange;

    private void Awake()
    {
        // The trigger volume must never physically block the player.
        GetComponent<Collider2D>().isTrigger = true;
        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    private void Update()
    {
        // HARD GATE: while any conversation is on screen this NPC is inert.
        // The manager keeps IsDialogueActive true for one frame after closing,
        // which outlasts PlayerInput's latched InteractPressed — so the press
        // that ends a conversation can never immediately restart it.
        if (DialogueManager.IsDialogueActive)
        {
            SetPrompt(false);
            return;
        }

        SetPrompt(isPlayerInRange);

        if (!isPlayerInRange || input == null) return;

        if (input.InteractPressed && DialogueManager.Instance != null)
            DialogueManager.Instance.StartDialogue(dialogue);
    }

    private void SetPrompt(bool visible)
    {
        if (interactPrompt != null && interactPrompt.activeSelf != visible)
            interactPrompt.SetActive(visible);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        input           = other.GetComponent<PlayerInput>();
        isPlayerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        isPlayerInRange = false;
        input           = null;
        SetPrompt(false);
    }
}
