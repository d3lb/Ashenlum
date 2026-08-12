using UnityEngine;

public class SecretaryBirdArena : MonoBehaviour
{
    [Header("Bounds (offset from this transform)")]
    [SerializeField] private Vector2 areaCenter = Vector2.zero;
    [SerializeField] private Vector2 areaSize = new Vector2(24f, 12f);

    [Header("Insets")]
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

    public int SideOf(float x) => x >= CenterX ? 1 : -1;

    public int FurthestWallFrom(Vector2 p)
    {
        float toLeft  = Mathf.Abs(p.x - LeftX);
        float toRight = Mathf.Abs(p.x - RightX);
        return toRight >= toLeft ? 1 : -1;
    }

    public int NearestWallFrom(Vector2 p) => -FurthestWallFrom(p);

    public float WallX(int side) => side > 0 ? RightX : LeftX;

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
