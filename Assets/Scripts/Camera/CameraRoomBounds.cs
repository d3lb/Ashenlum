using UnityEngine;

// One of these sits in every room. It tells CameraManager how that room wants to be filmed.
[RequireComponent(typeof(BoxCollider2D))]
public class CameraRoomBounds : MonoBehaviour {
    [Header("Refrences")]
    [SerializeField] private CameraManager cameraManager;

    [Header("Look")]
    [SerializeField] private bool allowLookUp;
    [SerializeField] private bool allowLookDown;

    // Down is entered positive; CameraManager negates it.
    [SerializeField] private float lookUpDistance = 6f;
    [SerializeField] private float lookDownDistance = 6f;

    [Header("Settings")]
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

    public bool AllowLookUp => allowLookUp;
    public bool AllowLookDown => allowLookDown;

    public float LookUpDistance => lookUpDistance;
    public float LookDownDistance => lookDownDistance;


    public bool LockX => lockX;

    public float MinX => minX;
    public float MaxX => maxX;

    public bool LockY => lockY;
    public float MinY => minY;
    public float MaxY => maxY;

    private void OnTriggerStay2D(Collider2D other) {
        if (!other.CompareTag("Player"))
            return;
        cameraManager.SetRoom(this);
    }
}