using UnityEngine;

/// <summary>
/// The static room the boss fights in. Put this on an empty GameObject in the arena
/// and size the rect with the gizmo.
///
/// Everything the boss does - perching, dashing, clamping, choosing a wall - is expressed
/// in terms of this rect. That means NO raycasting and NO runtime wall detection anywhere
/// in the fight. Since the room never changes, the walls are just two numbers.
/// </summary>
public class SecretaryBirdArena : MonoBehaviour
{
    [Header("Bounds (offset from this transform)")]
    [SerializeField] private Vector2 areaCenter = Vector2.zero;
    [SerializeField] private Vector2 areaSize = new Vector2(24f, 12f);

    [Header("Insets")]
    [Tooltip("How far off the wall surface the boss perches.")]
    [SerializeField] private float wallInset = 0.8f;
    [SerializeField] private float floorInset = 0.6f;
    [SerializeField] private float ceilingInset = 0.6f;

    public Vector2 Center => (Vector2)transform.position + areaCenter;

    public float LeftX  => Center.x - areaSize.x * 0.5f + wallInset;
    public float RightX => Center.x + areaSize.x * 0.5f - wallInset;
    public float FloorY => Center.y - areaSize.y * 0.5f + floorInset;
    public float CeilY  => Center.y + areaSize.y * 0.5f - ceilingInset;

    public float CenterX => (LeftX + RightX) * 0.5f;
    public float Width   => RightX - LeftX;

    /// <summary>+1 = right wall, -1 = left wall.</summary>
    public int SideOf(float x) => x >= CenterX ? 1 : -1;

    /// <summary>
    /// The wall the player is furthest from. This is the default anchor for every
    /// wall attack: it maximises the boss's runway AND the player's reaction time,
    /// so a very fast dash still reads as fair.
    /// </summary>
    public int FurthestWallFrom(Vector2 p)
    {
        float toLeft  = Mathf.Abs(p.x - LeftX);
        float toRight = Mathf.Abs(p.x - RightX);
        return toRight >= toLeft ? 1 : -1;
    }

    public int NearestWallFrom(Vector2 p) => -FurthestWallFrom(p);

    public float WallX(int side) => side > 0 ? RightX : LeftX;

    /// <summary>heightT: 0 = floor, 1 = ceiling.</summary>
    public Vector2 Perch(int side, float heightT)
        => new Vector2(WallX(side), Mathf.Lerp(FloorY, CeilY, Mathf.Clamp01(heightT)));

    public float HeightTOf(float y) => Mathf.InverseLerp(FloorY, CeilY, y);

    public float ClampX(float x) => Mathf.Clamp(x, LeftX, RightX);
    public float ClampY(float y) => Mathf.Clamp(y, FloorY, CeilY);

    public Vector2 Clamp(Vector2 p) => new Vector2(ClampX(p.x), ClampY(p.y));

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.45f, 0.1f, 0.45f);
        Gizmos.DrawWireCube(Center, areaSize);

        // Playable perch box
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);
        Vector3 bl = new Vector3(LeftX,  FloorY);
        Vector3 tl = new Vector3(LeftX,  CeilY);
        Vector3 br = new Vector3(RightX, FloorY);
        Vector3 tr = new Vector3(RightX, CeilY);
        Gizmos.DrawLine(bl, tl);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(tl, tr);
    }
}
