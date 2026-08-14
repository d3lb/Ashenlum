using UnityEngine;

// Anything the player can press E on.

[RequireComponent(typeof(Collider2D))]
public abstract class Interactable : MonoBehaviour
{
    private InteractPrompt prompt;
    private PlayerInput input;
    private bool inRange;

    protected virtual void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;

        prompt = GetComponentInChildren<InteractPrompt>(true);
        if (prompt == null)
            Debug.LogError($"[{GetType().Name}] '{name}' has no InteractPrompt child.", this);
    }

    private void Update()
    {
        bool available = inRange && input != null && CanInteract;

        if (prompt != null)
        {
            if (available) prompt.Show(PromptVerb);
            else           prompt.Hide();
        }

        if (available && input.InteractPressed) Interact();
    }

    protected virtual bool CanInteract => true;
    protected virtual string PromptVerb => "Interact";

    protected abstract void Interact();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        input = other.GetComponent<PlayerInput>();
        inRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        inRange = false;
        input = null;
    }
}
