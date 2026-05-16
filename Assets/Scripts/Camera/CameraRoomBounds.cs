using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CameraRoomBounds : MonoBehaviour
{
    [Header("Refrences")]
    [SerializeField] private CameraManager cameraManager;

    [Header("Settings")]
    [SerializeField] private float trackedYOffset;
    [SerializeField] private float deadZoneHeight;
    [Space(3)]
    [SerializeField] private bool lockX;
    [SerializeField] private float minX;
    [SerializeField] private float maxX;
    [Space(3)]
    [SerializeField] private bool lockY;
    [SerializeField] private bool lockYAfterGrounded;
    [SerializeField] private float minY;
    [SerializeField] private float maxY;

    private BoxCollider2D box;


    // Exposed room settings for use in camera manager
    public float TrackedYOffset => trackedYOffset;
    public float DeadZoneHeight => deadZoneHeight;

    public bool LockX => lockX;
    public float MinX => minX;
    public float MaxX => maxX;

    public bool LockY => lockY;
    public bool LockYAfterGrounded => lockYAfterGrounded;
    public float MinY => minY;
    public float MaxY => maxY;

    private void Awake()
    {
        box = GetComponent<BoxCollider2D>();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        cameraManager.SetRoom(this);
    }
}