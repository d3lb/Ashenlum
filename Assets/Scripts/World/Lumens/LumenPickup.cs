using UnityEngine;
using System.Collections;

public class LumenPickup : MonoBehaviour {

    [Header("Refrence")]
    [SerializeField] private Collider2D colli;

    [Header("Worth")]
    // The shade splits a purse across a handful of these instead of hundreds.
    [SerializeField] private int value = 1;

    [Header("Attraction")]
    [SerializeField] private float attractRadius = 3f;     // how close player must be to trigger fly
    [SerializeField] private float spawnDelay = 1f;        // time after which the lumen starts following the player

    [Header("Fly Settings")]
    [SerializeField] private float initialFlySpeed = 3f;   // starting speed when it begins flying
    [SerializeField] private float acceleration = 8f;      // how fast it speeds up

    [Header("Spawn Pop")]
    [SerializeField] private float popForceX = 3f;
    [SerializeField] private float popForceY = 5f;

    private Transform player;
    private Rigidbody2D rb;
    private bool isFlying = false;
    private bool forceFly = false;
    private float currentSpeed;
    private float spawnTimer = 0f;

    // Already on its way to the player, so a rest is not allowed to take it back.
    public bool IsFlying => isFlying;

    public void SetValue(int amount) => value = Mathf.Max(1, amount);

    // The shade's light is already the player's - it comes home rather than waiting.
    public void LaunchAtPlayer() => forceFly = true;

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start() {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        rb.gravityScale = 1f;

        float randomX = Random.Range(-popForceX, popForceX);
        rb.AddForce(new Vector2(randomX, popForceY), ForceMode2D.Impulse);

    }

    private void Update() {
        if (player == null) return;

        if (!isFlying) spawnTimer += Time.deltaTime;

        float dist = Vector2.Distance(transform.position, player.position);

        if (!isFlying && (forceFly || (spawnTimer >= spawnDelay && dist <= attractRadius))) {
            isFlying = true;
            currentSpeed = initialFlySpeed;
            rb.gravityScale = 0f;
            colli.enabled = false;
            rb.linearVelocity = Vector2.zero;
        }

        if (!isFlying) return;

        currentSpeed += acceleration * Time.deltaTime;

        transform.position = Vector2.MoveTowards(
            transform.position,
            player.position,
            currentSpeed * Time.deltaTime
        );
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (!other.CompareTag("Player")) return;

        GameManager.Instance.AddLumens(value);

        // Positional: a pile collected across the room should not be as loud as one
        // picked up at your feet.
        SoundManager.PlayAt(SoundId.Pickup, transform.position);

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attractRadius);
    }
}