using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CameraRoomBounds : MonoBehaviour
{
    [SerializeField] private CameraManager cameraManager;
    private BoxCollider2D box;

    private void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
    }
}