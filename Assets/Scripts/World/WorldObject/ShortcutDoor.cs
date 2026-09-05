using UnityEngine;

// Opens once, from one side only, and stays open forever.
public class ShortcutDoor : Interactable {
    [SerializeField] private string doorId;

    // Must be on a child: Interactable turns this object's collider into a trigger.
    [SerializeField] private Collider2D blocker;

    [SerializeField] private SpriteRenderer visual;

    [SerializeField] private Animator animator;

    [Header("Side")]
    [SerializeField] private bool opensFromRight = true;
    [SerializeField] private string wrongSideMessage = "Does not open from this side";
    [SerializeField] private float wrongSideMessageTime = 1.5f;

    private bool opened;
    private Collider2D range;

    protected override void Awake() {
        base.Awake();
        range = GetComponent<Collider2D>();

        if (string.IsNullOrEmpty(doorId))
            Debug.LogError($"[ShortcutDoor] '{name}' has no Door Id, so it cannot be saved.", this);
    }

    // Start, not Awake, so GameManager is up.
    private void Start() {
        opened = GameManager.Instance != null && GameManager.Instance.HasSeenEvent(doorId);
        Apply();
    }

    // From the trigger, not the pivot - the pivot is often outside the doorway.
    private float DividingX => range != null ? range.bounds.center.x : transform.position.x;

    private bool OnOpeningSide =>
        Player != null && (opensFromRight ? Player.position.x > DividingX : Player.position.x < DividingX);

    // Openable from both sides so the prompt reads the same; the side is judged on press.
    protected override bool CanInteract => !opened;
    protected override string PromptVerb => "Open";

    protected override void Interact() {
        if (!OnOpeningSide) {
            Toast.Show(wrongSideMessage, wrongSideMessageTime);
            return;
        }

        opened = true;
        GameManager.Instance.RegisterEvent(doorId);
        Apply();
    }

    private void Apply() {
        if (blocker != null) blocker.enabled = !opened;

        if (animator != null) animator.SetBool("IsOpen", opened);
        else if (visual != null) visual.enabled = !opened;
    }

    // Red is the split, green is the opening side.
    private void OnDrawGizmos() {
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
