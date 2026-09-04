using UnityEngine;

public class Corpse : MonoBehaviour {
    [SerializeField] private float popForce = 3f;
    [SerializeField] private float upForce = 2f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Settle")]
    [SerializeField] private float settleTime = 0.1f;

    private Rigidbody2D rb;
    private bool landed;
    private float settleTimer;

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Pop(Vector2 direction) {
        rb.AddForce(new Vector2(Mathf.Sign(direction.x) * popForce, upForce), ForceMode2D.Impulse);
    }

    private void Update() {
        if (landed)
            return;

        bool onGround = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);

        if (onGround) {
            settleTimer += Time.deltaTime;

            if (settleTimer >= settleTime)
                Freeze();
        }
        else {
            settleTimer = 0f;
        }
    }

    private void Freeze() {
        landed = true;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Static;
    }
}