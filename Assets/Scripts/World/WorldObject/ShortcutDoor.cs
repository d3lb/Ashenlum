using UnityEngine;

// Opens once, from one side only, and stays open forever.
public class ShortcutDoor : Interactable
{
    [SerializeField] private string doorId;

    // The solid collider. Must be on a child: Interactable turns the collider on this
    // object into a trigger, and with two here it may pick the wrong one.
    [SerializeField] private Collider2D blocker;

    [SerializeField] private SpriteRenderer visual;

    // Optional. Set one and it owns the picture instead of the sprite being switched off.
    [SerializeField] private Animator animator;

    [Header("Side")]
    [SerializeField] private bool opensFromRight = true;
    [SerializeField] private string wrongSideMessage = "Does not open from this side";

    private bool opened;
    private Collider2D range;

    protected override void Awake()
    {
        base.Awake();
        range = GetComponent<Collider2D>();

        if (string.IsNullOrEmpty(doorId))
            Debug.LogError($"[ShortcutDoor] '{name}' has no Door Id, so it cannot be saved.", this);
    }

    // Start, not Awake, so GameManager is up - same as BreakableWall.
    private void Start()
    {
        opened = GameManager.Instance != null && GameManager.Instance.HasSeenEvent(doorId);
        Apply();
    }

    // Measured from the trigger, not transform.position. The root pivot is often not in
    // the doorway, and then every approach is on the same side of it and both read wrong.
    private float DividingX => range != null ? range.bounds.center.x : transform.position.x;

    private bool OnOpeningSide =>
        Player != null && (opensFromRight ? Player.position.x > DividingX
                                          : Player.position.x < DividingX);

    protected override bool CanInteract => !opened && OnOpeningSide;
    protected override string PromptVerb => "Open";
    protected override string BlockedMessage => opened ? null : wrongSideMessage;

    protected override void Interact()
    {
        opened = true;
        GameManager.Instance.RegisterEvent(doorId);
        Apply();
    }

    private void Apply()
    {
        if (blocker != null) blocker.enabled = !opened;

        if (animator != null) animator.SetBool("IsOpen", opened);
        else if (visual != null) visual.enabled = !opened;
    }

    // Red line is the split, green ball is the side that opens it. If the red line is
    // not in the doorway, that is the bug.
    private void OnDrawGizmos()
    {
        Collider2D c = range != null ? range : GetComponent<Collider2D>();
        float x = c != null ? c.bounds.center.x : transform.position.x;
        float y = transform.position.y;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(x, y - 2f, 0f), new Vector3(x, y + 2f, 0f));

        Gizmos.color = Color.green;
        Vector3 side = new Vector3(x + (opensFromRight ? 1.5f : -1.5f), y, 0f);
        Gizmos.DrawLine(new Vector3(x, y, 0f), side);
        Gizmos.DrawSphere(side, 0.15f);
    }
}
