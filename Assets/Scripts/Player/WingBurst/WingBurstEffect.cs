using UnityEngine;

public class WingBurstEffect : MonoBehaviour
{
    [SerializeField] private float lifeTime = 0.4f;
    [SerializeField] private float scaleMultiplier = 1.5f;

    private SpriteRenderer sprite;

    private Vector3 startScale;
    private Color startColor;

    private void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();

        startScale = transform.localScale;
        startColor = sprite.color;
    }

    private void Update()
    {
        lifeTime -= Time.deltaTime;

        float t = 1f - (lifeTime / 0.4f);

        transform.localScale =
            Vector3.Lerp(
                startScale,
                startScale * scaleMultiplier,
                t
            );

        Color c = startColor;

        c.a = 1f - t;

        sprite.color = c;

        if (lifeTime <= 0f)
            Destroy(gameObject);
    }
}