using UnityEngine;

public class SecretaryBirdShockwave : MonoBehaviour {
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private bool shrinkOverLife = true;

    private int dir = 1;
    private SecretaryBirdArena arena;
    private Vector3 baseScale;
    private float born;

    public void Launch(int direction, SecretaryBirdArena a) {
        dir = direction;
        arena = a;
        born = Time.time;
        baseScale = transform.localScale;

        Vector3 s = baseScale;
        s.x = Mathf.Abs(s.x) * dir;
        transform.localScale = s;
        baseScale = s;

        Destroy(gameObject, lifetime);
    }

    private void Update() {
        transform.position += Vector3.right * (dir * speed * Time.deltaTime);

        if (shrinkOverLife && lifetime > 0f) {
            float k = 1f - Mathf.Clamp01((Time.time - born) / lifetime);
            transform.localScale = new Vector3(baseScale.x, baseScale.y * k, baseScale.z);
        }

        if (arena == null) return;

        if (transform.position.x < arena.LeftX - 2f || transform.position.x > arena.RightX + 2f)
            Destroy(gameObject);
    }
}
