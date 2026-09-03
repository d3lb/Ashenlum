using System.Collections;
using UnityEngine;

// An expanding circle of light that hurts while you are inside it.
//
// Drawn with a LineRenderer rather than a sprite: a circle stretched from a square
// image looks wrong at every size, and a ring outline reads better anyway - you can
// see the edge coming at you.
public class KingRing : MonoBehaviour
{
    [SerializeField] private int segments = 48;

    private CircleCollider2D circle;
    private LineRenderer line;
    private ContactFilter2D filter;
    private readonly Collider2D[] results = new Collider2D[4];

    private int damage;
    private Color telegraphColor;
    private Color activeColor;

    // Its own tick, unlike KingLight. The player's 0.3s iFrames would otherwise set the
    // rate at 50 damage a second, which kills from full in two seconds inside.
    private float tickInterval;
    private float nextTick;

    private bool armed;

    public static KingRing Spawn(KingBrain brain, Transform follow, Vector2 offset,
                                 float maxRadius, float chargeTime, float growTime,
                                 float holdTime, int damage, float tickInterval)
    {
        GameObject go = new GameObject("KingRing");
        go.transform.position = (follow != null ? follow.position : Vector3.zero)
                                + (Vector3)offset;

        // Parented so it stays centred on him even though he does not move.
        if (follow != null) go.transform.SetParent(follow, true);

        CircleCollider2D circle = go.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = 0f;
        circle.enabled = false;

        KingRing ring = go.AddComponent<KingRing>();
        ring.circle = circle;
        ring.damage = damage;
        ring.tickInterval = Mathf.Max(0.05f, tickInterval);
        ring.telegraphColor = brain != null ? brain.TelegraphColor : Color.yellow;
        ring.activeColor = brain != null ? brain.ActiveColor : Color.white;

        ring.filter = new ContactFilter2D();
        ring.filter.SetLayerMask(brain != null ? brain.PlayerLayer : ~0);
        ring.filter.useTriggers = true;

        LineRenderer line = go.AddComponent<LineRenderer>();
        // Sprites/Default, or URP renders it magenta.
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.useWorldSpace = false;
        line.loop = true;
        line.widthMultiplier = 0.25f;
        line.startColor = line.endColor = ring.telegraphColor;

        if (brain != null)
        {
            line.sortingLayerName = brain.SortingLayer;
            line.sortingOrder = brain.SortingOrder;
        }

        ring.line = line;
        ring.StartCoroutine(ring.Run(maxRadius, chargeTime, growTime, holdTime));
        return ring;
    }

    private void Update()
    {
        if (!armed || circle == null || Time.time < nextTick) return;

        int count = circle.Overlap(filter, results);

        for (int i = 0; i < count; i++)
        {
            PlayerHealth player = results[i].GetComponentInParent<PlayerHealth>();
            if (player == null) continue;

            nextTick = Time.time + tickInterval;
            player.TakeDamage(damage, transform.position);
            break;
        }
    }

    private void Redraw(float radius)
    {
        if (line == null) return;

        int n = Mathf.Max(8, segments);
        line.positionCount = n;

        for (int i = 0; i < n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f;
            line.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius);
        }
    }

    private IEnumerator Run(float maxRadius, float chargeTime, float growTime, float holdTime)
    {
        // He glows before anything happens. This is the "get away from me" warning.
        float t = 0f;
        while (t < chargeTime)
        {
            t += Time.deltaTime;

            // A small pulse at full size, so the warning shows how far it will reach.
            Redraw(maxRadius * (0.9f + 0.1f * Mathf.Sin(t * 12f)));
            yield return null;
        }

        circle.enabled = true;
        armed = true;
        if (line != null) line.startColor = line.endColor = activeColor;

        t = 0f;
        while (t < growTime)
        {
            t += Time.deltaTime;
            float r = Mathf.Lerp(0f, maxRadius, growTime <= 0f ? 1f : t / growTime);

            circle.radius = r;
            Redraw(r);
            yield return null;
        }

        circle.radius = maxRadius;
        Redraw(maxRadius);

        if (holdTime > 0f) yield return new WaitForSeconds(holdTime);

        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        if (circle == null) return;

        Gizmos.color = armed ? new Color(1f, 0.3f, 0.1f, 0.9f)
                             : new Color(1f, 0.9f, 0.3f, 0.5f);

        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.05f, circle.radius));
    }
}
