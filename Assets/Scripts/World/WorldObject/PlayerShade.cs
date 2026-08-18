using UnityEngine;

// What is left of you where you fell - your own light, still hanging in the air. Get near
// it and it comes back on its own. Nothing to fight, nothing to press: the walk is the
// whole cost of dying.
public class PlayerShade : MonoBehaviour
{
    [Header("Return")]
    [SerializeField] private float attractRadius   = 3f;
    [SerializeField] private float initialSpeed    = 3f;
    [SerializeField] private float acceleration    = 8f;
    [SerializeField] private float collectDistance = 0.4f;

    private Transform player;
    private bool  returning;
    private float speed;

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    private void Update()
    {
        if (player == null) return;

        if (!returning)
        {
            if (Vector2.Distance(transform.position, player.position) > attractRadius) return;

            returning = true;
            speed = initialSpeed;
        }

        speed += acceleration * Time.deltaTime;

        transform.position = Vector2.MoveTowards(
            transform.position, player.position, speed * Time.deltaTime);

        if (Vector2.Distance(transform.position, player.position) <= collectDistance)
            Collect();
    }

    private void Collect()
    {
        if (GameManager.Instance != null) GameManager.Instance.CollectShade();
        Destroy(gameObject);
    }
}
