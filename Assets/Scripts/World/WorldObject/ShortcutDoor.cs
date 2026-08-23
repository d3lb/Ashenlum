using UnityEngine;

// A gate that only opens from the far side, and stays open once it has been.
// The long way round is the price of the shortcut.
public class ShortcutDoor : Interactable
{
    [SerializeField] private string doorId;

    [Header("Parts")]
    // Left on until the door opens. Separate from the trigger this script needs.
    [SerializeField] private Collider2D blocker;
    [SerializeField] private GameObject closedVisual;
    [SerializeField] private GameObject openVisual;

    [Header("Side")]
    // The side the player has to be standing on to open it.
    [SerializeField] private bool opensFromRight = true;

    private bool opened;

    protected override void Awake()
    {
        base.Awake();

        if (string.IsNullOrEmpty(doorId))
            Debug.LogError($"[ShortcutDoor] '{name}' has no Door Id, so it cannot be saved.", this);
    }

    // Start, not Awake, so GameManager is guaranteed up - same as BreakableWall.
    private void Start()
    {
        bool already = GameManager.Instance != null && GameManager.Instance.HasSeenEvent(doorId);
        Apply(already);
    }

    private bool OnOpeningSide
    {
        get
        {
            if (Player == null) return false;

            return opensFromRight
                ? Player.position.x > transform.position.x
                : Player.position.x < transform.position.x;
        }
    }

    // From the wrong side there is no prompt at all, rather than a prompt that refuses.
    protected override bool CanInteract => !opened && OnOpeningSide;

    protected override string PromptVerb => "Open";

    protected override void Interact()
    {
        GameManager.Instance.RegisterEvent(doorId);
        Apply(true);
    }

    private void Apply(bool isOpen)
    {
        opened = isOpen;

        if (blocker != null)       blocker.enabled = !isOpen;
        if (closedVisual != null)  closedVisual.SetActive(!isOpen);
        if (openVisual != null)    openVisual.SetActive(isOpen);
    }

    // Green arrow points at the side you can open it from.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        Vector3 from = transform.position;
        Vector3 to = from + Vector3.right * (opensFromRight ? 1.5f : -1.5f);

        Gizmos.DrawLine(from, to);
        Gizmos.DrawSphere(to, 0.15f);
    }
}
