using UnityEngine;

public class KingArena : MonoBehaviour {
    [Header("Bounds (offset from this transform)")]
    [SerializeField] private Vector2 areaCenter = Vector2.zero;
    [SerializeField] private Vector2 areaSize = new Vector2(30f, 16f);

    public Vector2 Center => (Vector2)transform.position + areaCenter;

    public float LeftX => Center.x - areaSize.x * 0.5f;
    public float RightX => Center.x + areaSize.x * 0.5f;
    public float FloorY => Center.y - areaSize.y * 0.5f;
    public float CeilY => Center.y + areaSize.y * 0.5f;

    public float Width => RightX - LeftX;
    public float Height => CeilY - FloorY;

    public float ClampX(float x) => Mathf.Clamp(x, LeftX, RightX);

    public float SlotX(int index, int count) =>
        count <= 1 ? Center.x : Mathf.Lerp(LeftX, RightX, index / (float)(count - 1));

    private void OnDrawGizmos() {
        Gizmos.color = new Color(1f, 0.9f, 0.4f, 0.6f);
        Gizmos.DrawWireCube(Center, areaSize);
    }
}
