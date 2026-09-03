using System.Collections;
using UnityEngine;

// One piece of the King's light: warn, then hurt, then go away.
//
// Built entirely in code. With no sprite assigned on the brain it draws a generated
// white square, so the fight is visible and tunable before any art exists.
public class KingLight : MonoBehaviour
{
    private static Sprite fallbackSprite;

    // One 1x1 white pixel stretched by the visual's scale. Made once, reused forever.
    public static Sprite FallbackSprite
    {
        get
        {
            if (fallbackSprite != null) return fallbackSprite;

            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();

            fallbackSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f),
                                           new Vector2(0.5f, 0.5f), 1f);
            return fallbackSprite;
        }
    }

    private BoxCollider2D box;
    private SpriteRenderer visual;
    private ContactFilter2D filter;
    private readonly Collider2D[] results = new Collider2D[4];

    private int damage;
    private Color activeColor;
    private bool armed;

    public static KingLight Spawn(KingBrain brain, Vector2 position, Vector2 size,
                                  float angle, float telegraphTime, float activeTime)
    {
        GameObject go = new GameObject("KingLight");
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));

        BoxCollider2D box = go.AddComponent<BoxCollider2D>();
        box.size = size;
        box.isTrigger = true;
        box.enabled = false;

        KingLight light = go.AddComponent<KingLight>();
        light.box = box;
        light.damage = brain != null ? brain.LightDamage : 15;
        light.activeColor = brain != null ? brain.ActiveColor : Color.white;

        light.filter = new ContactFilter2D();
        light.filter.SetLayerMask(brain != null ? brain.PlayerLayer : ~0);
        light.filter.useTriggers = true;

        // On a child, so scaling the picture never resizes the collider with it.
        GameObject art = new GameObject("Visual");
        art.transform.SetParent(go.transform, false);
        art.transform.localScale = new Vector3(size.x, size.y, 1f);

        light.visual = art.AddComponent<SpriteRenderer>();
        light.visual.sprite = brain != null && brain.LightSprite != null
            ? brain.LightSprite : FallbackSprite;
        light.visual.color = brain != null ? brain.TelegraphColor : Color.yellow;

        if (brain != null)
        {
            light.visual.sortingLayerName = brain.SortingLayer;
            light.visual.sortingOrder = brain.SortingOrder;
        }

        light.StartCoroutine(light.Run(telegraphTime, activeTime));
        return light;
    }

    private void Update()
    {
        if (!armed || box == null) return;

        int count = box.Overlap(filter, results);

        for (int i = 0; i < count; i++)
        {
            PlayerHealth player = results[i].GetComponentInParent<PlayerHealth>();

            // No cooldown here on purpose - the player's own iFrames already stop a
            // second hit, and a duplicate rule would drift out of sync with that one.
            if (player != null) player.TakeDamage(damage, transform.position);
        }
    }

    private IEnumerator Run(float telegraphTime, float activeTime)
    {
        // Collider stays off through the warning, so a telegraph can never damage you.
        if (telegraphTime > 0f) yield return new WaitForSeconds(telegraphTime);

        if (visual != null) visual.color = activeColor;
        box.enabled = true;
        armed = true;

        if (activeTime > 0f) yield return new WaitForSeconds(activeTime);

        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        if (box == null) return;

        Gizmos.color = armed ? new Color(1f, 0.2f, 0.2f, 0.9f)
                             : new Color(1f, 0.9f, 0.3f, 0.5f);

        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(box.offset, box.size);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
