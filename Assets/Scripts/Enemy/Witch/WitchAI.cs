using System.Collections;
using UnityEngine;

// Fires from a distance and blinks away when crowded. Blinking and firing share one busy
// flag, so pressure is the whole counterplay: keep him blinking and he never gets a shot off.
public class WitchAI : MonoBehaviour, IRespawnReset {
    [Header("References")]
    [SerializeField] private EnemyHealth health;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Animator animator;

    // Two, but any number works - he always takes the one furthest from the player.
    [Header("Blink spots")]
    [SerializeField] private Transform[] spots;

    [Header("Shot")]
    [SerializeField] private WitchProjectile projectilePrefab;

    // Mirrored by facing, so one offset covers both sides and nothing has to be flipped.
    [SerializeField] private Vector2 spawnOffset = new Vector2(0.6f, 0.2f);
    [SerializeField] private float shotSpeed = 14f;
    [SerializeField] private int shotDamage = 1;
    [SerializeField] private float shotInterval = 2f;

    // The tell. Closing the gap inside this window is what cancels the shot.
    [SerializeField] private float aimTime = 0.6f;

    // Outside this he ignores you entirely, instead of sniping from across the room.
    [SerializeField] private float sightRadius = 12f;

    [Header("Blink")]
    [SerializeField] private float panicRadius = 5f;
    [SerializeField] private float vanishTime = 0.2f;
    [SerializeField] private float appearTime = 0.4f;

    // He must stand still this long after landing. This is the whole window you get to hit
    // him in - without it he blinks the moment you arrive and can never be caught.
    [SerializeField] private float blinkCooldown = 1.2f;

    [SerializeField] private GameObject blinkEffect;

    [Header("Reward")]
    [SerializeField] private ActiveAbility drops;

    private Transform player;
    private int spot;
    private bool busy;
    private float nextShotAt;
    private float canBlinkAt;

    private void Awake() {
        if (health == null) health = GetComponent<EnemyHealth>();
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();

        if (health != null) health.OnDied += Drop;
    }

    private void OnDestroy() {
        if (health != null) health.OnDied -= Drop;
    }

    private void Start() {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        if (spots != null && spots.Length > 0) transform.position = spots[0].position;
    }

    // Coroutines died with the object mid-blink, so busy stays true and the sprite stays
    // hidden - he comes back invisible and inert without this.
    public void ResetForRespawn() {
        StopAllCoroutines();

        busy = false;
        nextShotAt = 0f;
        canBlinkAt = 0f;
        spot = 0;

        if (sprite != null) sprite.enabled = true;
        if (spots != null && spots.Length > 0) transform.position = spots[0].position;
    }

    private void Drop() {
        if (drops != null && GameManager.Instance != null) GameManager.Instance.GrantAbility(drops);
    }

    private float ToPlayer =>
        player == null ? float.MaxValue : Vector2.Distance(transform.position, player.position);

    private bool Crowded => ToPlayer <= panicRadius;
    private bool InSight => ToPlayer <= sightRadius;

    private void Update() {
        if (player == null || busy) return;

        Face();

        if (Crowded && Time.time >= canBlinkAt) {
            StartCoroutine(Blink());
            return;
        }

        if (InSight && Time.time >= nextShotAt) StartCoroutine(Shoot());
    }

    private void Face() {
        if (sprite == null) return;
        sprite.flipX = player.position.x < transform.position.x;
    }

    private IEnumerator Shoot() {
        busy = true;

        if (animator != null) animator.SetTrigger("Aim");

        // Aborts on approach rather than firing anyway - otherwise rushing him is punished.
        float t = 0f;
        while (t < aimTime) {
            if (Crowded) {
                busy = false;
                yield break;
            }

            t += Time.deltaTime;
            yield return null;
        }

        Face();
        Fire();

        nextShotAt = Time.time + shotInterval;
        busy = false;
    }

    private bool FacingRight => sprite == null || !sprite.flipX;

    private Vector3 MuzzlePosition =>
        transform.position +
        new Vector3(Mathf.Abs(spawnOffset.x) * (FacingRight ? 1f : -1f), spawnOffset.y, 0f);

    private void Fire() {
        if (projectilePrefab == null) return;

        WitchProjectile shot = Instantiate(projectilePrefab, MuzzlePosition, Quaternion.identity);
        shot.Launch(FacingRight ? Vector2.right : Vector2.left, shotSpeed, shotDamage);
    }

    private IEnumerator Blink() {
        busy = true;

        if (animator != null) animator.SetTrigger("Blink");
        Poof();

        if (sprite != null) sprite.enabled = false;
        yield return new WaitForSeconds(vanishTime);

        spot = FurthestSpot();
        if (spots != null && spots.Length > 0) transform.position = spots[spot].position;

        if (sprite != null) sprite.enabled = true;
        Poof();

        yield return new WaitForSeconds(appearTime);

        // Ready now, not in shotInterval: arriving and immediately committing to an aim is
        // what gives you something to run at.
        nextShotAt = Time.time;
        canBlinkAt = Time.time + blinkCooldown;

        busy = false;
    }

    private void Poof() {
        if (blinkEffect != null) Instantiate(blinkEffect, transform.position, Quaternion.identity);
    }

    private int FurthestSpot() {
        if (spots == null || spots.Length == 0) return 0;

        int best = spot;
        float bestDistance = -1f;

        for (int i = 0; i < spots.Length; i++) {
            if (spots[i] == null) continue;

            float d = Vector2.Distance(spots[i].position, player.position);
            if (d <= bestDistance) continue;

            bestDistance = d;
            best = i;
        }

        return best;
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, panicRadius);

        if (spots == null) return;

        Gizmos.color = Color.cyan;
        foreach (Transform t in spots)
            if (t != null) Gizmos.DrawWireSphere(t.position, 0.4f);

        // Both muzzles, so the offset can be placed without entering play mode.
        Gizmos.color = Color.red;
        Vector3 up = new Vector3(0f, spawnOffset.y, 0f);
        Gizmos.DrawWireSphere(transform.position + up + Vector3.right * Mathf.Abs(spawnOffset.x), 0.15f);
        Gizmos.DrawWireSphere(transform.position + up + Vector3.left * Mathf.Abs(spawnOffset.x), 0.15f);
    }
}
