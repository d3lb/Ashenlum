using UnityEngine;

// Anything the player can press E on.

[RequireComponent(typeof(Collider2D))]
public abstract class Interactable : MonoBehaviour
{
    private InteractPrompt prompt;
    private PlayerInput input;
    private bool inRange;
    private Transform player;

    protected virtual void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;

        prompt = GetComponentInChildren<InteractPrompt>(true);
        if (prompt == null)
            Debug.LogError($"[{GetType().Name}] '{name}' has no InteractPrompt child.", this);
    }

    private void Update()
    {
        // Update runs at timeScale 0, so without this an open panel still passes E
        // through to whatever the player happens to be standing on.
        bool nearby = inRange && input != null && !UIState.Busy;
        bool available = nearby && CanInteract;

        if (prompt != null)
        {
            if (available)
                prompt.Show(PromptVerb);
            else if (nearby && !string.IsNullOrEmpty(BlockedMessage))
                prompt.ShowMessage(BlockedMessage);
            else
                prompt.Hide();
        }

        if (available && input.InteractPressed) Interact();
    }

    protected virtual bool CanInteract => true;
    protected virtual string PromptVerb => "Interact";

    // Shown instead of the prompt when you are close but cannot act. Null stays silent,
    // which is right for most things - a locked door is worth explaining, a shop is not.
    protected virtual string BlockedMessage => null;

    protected bool PlayerInRange => inRange;

    // Null unless the player is inside the trigger. For things that care which side.
    protected Transform Player => player;

    protected abstract void Interact();

    // For things that care about proximity itself, not just the E press.
    protected virtual void OnPlayerEnter() { }
    protected virtual void OnPlayerExit() { }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        input = other.GetComponent<PlayerInput>();
        player = other.transform;
        inRange = true;
        OnPlayerEnter();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        inRange = false;
        input = null;
        player = null;
        OnPlayerExit();
    }
}
