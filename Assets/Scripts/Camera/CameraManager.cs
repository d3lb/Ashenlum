using UnityEngine;
using Cinemachine;
using System.Collections;
using static PlayerState;

[DefaultExecutionOrder(100)]
public class CameraManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerState state;
    [SerializeField] private CinemachineVirtualCamera cam;
    [SerializeField] private Transform player;
    private CinemachineFramingTransposer transposer;
    private CameraRoomBounds currentRoom;

    public CameraRoomBounds CurrentRoom => currentRoom;

    [Header("X Offset Settings")]
    [SerializeField] private float flipSpeed = 5f;
    [SerializeField] private float howFar = 1f;

    [Header("X Offset Settings")]
    private float yOffsetSpeed = 7f;

    [Header("Look Up/Down Settings")]
    private float lookDistance = 6f;
    private float lookSnapSpeed = 40f;

    private float lookOffsetY;
    private bool forcingYOffset;

    private float currentLookSpeed;
    private float yVelocity;
    private float lookVelocity;
    private void Awake()
    {
        transposer = cam.GetCinemachineComponent<CinemachineFramingTransposer>();
    }

    private void LateUpdate()
    {
        xAxis();
        yAxis();
    }

    private void xAxis()
    {
        if (currentRoom == null) return;

        float desiredOffsetX = state.IsFacingRight ? howFar : -howFar;
        float tempFlipSpeed = state.IsGrounded ? flipSpeed : flipSpeed / 2f;
        Vector3 offset = transposer.m_TrackedObjectOffset;

        offset.x = Mathf.Lerp(offset.x, desiredOffsetX, tempFlipSpeed * Time.deltaTime);

        bool edgeLocked = false;
        if (currentRoom.LockX)
        {
            float playerX = player.position.x;
            if (playerX >= currentRoom.MaxX || playerX <= currentRoom.MinX)
                edgeLocked = true;
        }

        if (edgeLocked)
        {
            transposer.m_SoftZoneWidth = 2f;
            transposer.m_DeadZoneWidth = 2f;
        }
        else
        {
            transposer.m_SoftZoneWidth = Mathf.Lerp(transposer.m_SoftZoneWidth, 0.2f, 7f * Time.deltaTime);
            transposer.m_DeadZoneWidth = Mathf.Lerp(transposer.m_DeadZoneWidth, 0f, 7f * Time.deltaTime);
        }

        transposer.m_TrackedObjectOffset = offset;
    }

    private void yAxis()
    {
        if (currentRoom == null) return;

        Vector3 offset = transposer.m_TrackedObjectOffset;
        float roomY = currentRoom.TrackedYOffset;

        if (forcingYOffset)
        {
            offset.y = Mathf.SmoothDamp(offset.y, roomY, ref yVelocity, 1f / yOffsetSpeed);
            transposer.m_TrackedObjectOffset = offset;
            transposer.m_SoftZoneHeight = 2f;
            transposer.m_DeadZoneHeight = 0f;

            if (Mathf.Abs(offset.y - roomY) > 0.05f)
                return;

            forcingYOffset = false;
        }

        float baseY = Mathf.SmoothDamp(offset.y, roomY, ref yVelocity, 1f / yOffsetSpeed);

        float targetY = roomY + lookOffsetY;
        offset.y = Mathf.SmoothDamp(baseY, targetY, ref lookVelocity, 1f / currentLookSpeed);
        transposer.m_TrackedObjectOffset = offset;

        bool edgeLocked = false;
        if (currentRoom.LockY)
        {
            float playerY = player.position.y;
            if (playerY >= currentRoom.MaxY || playerY <= currentRoom.MinY)
                edgeLocked = true;
        }

        if (edgeLocked)
        {
            transposer.m_SoftZoneHeight = 2f;
            transposer.m_DeadZoneHeight = 2f;
        }
        else
        {
            transposer.m_SoftZoneHeight = Mathf.Lerp(transposer.m_SoftZoneHeight, 0.5f, 7f * Time.deltaTime);
            transposer.m_DeadZoneHeight = Mathf.Lerp(transposer.m_DeadZoneHeight, 0f, 7f * Time.deltaTime);
        }
    }

    public void SetRoom(CameraRoomBounds room)
    {
        if (currentRoom == room) return;
        currentRoom = room;
        forcingYOffset = true;
    }

    // Distance comes from the room. lookDistance is only a fallback for a caller that
    // somehow looks with no room set.
    public void LookUp()
    {
        lookOffsetY = currentRoom != null ? currentRoom.LookUpDistance : lookDistance;
        currentLookSpeed = lookSnapSpeed;
        lookVelocity = 0f;
    }

    public void LookDown()
    {
        lookOffsetY = -(currentRoom != null ? currentRoom.LookDownDistance : lookDistance);
        currentLookSpeed = lookSnapSpeed;
        lookVelocity = 0f;
    }
    public void ResetLook() { lookOffsetY = 0f; currentLookSpeed = lookSnapSpeed; }
}