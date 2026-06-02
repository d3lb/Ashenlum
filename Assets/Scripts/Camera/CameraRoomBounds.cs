using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CameraRoomBounds : MonoBehaviour
{
    [Header("Refrences")]
    [SerializeField] private CameraManager cameraManager;

    [Header("Settings")]
    [SerializeField] private bool forceYOffset;
    [SerializeField] private float trackedYOffset;

    [Header("X Lock")]
    [SerializeField] private bool lockX;
    [SerializeField] private float minX;
    [SerializeField] private float maxX;

    [Header("Y Lock")]
    [SerializeField] private bool lockY;
    [SerializeField] private float minY;
    [SerializeField] private float maxY;

    // Exposed room settings for use in camera manager
    public float TrackedYOffset => trackedYOffset;
    public bool ForceYOffset => forceYOffset;
    public bool LockX => lockX;
    public float MinX => minX;
    public float MaxX => maxX;

    public bool LockY => lockY;
    public float MinY => minY;
    public float MaxY => maxY;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;
        cameraManager.SetRoom(this);
    }
}