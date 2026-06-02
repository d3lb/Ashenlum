using UnityEngine;
using Cinemachine;
using System.Collections;
using static PlayerState;
using Unity.VisualScripting;

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

    private bool forcingYOffset;
    private Transform proxyTarget;

    private void Awake()
    {
        transposer = cam.GetCinemachineComponent<CinemachineFramingTransposer>();
        proxyTarget = new GameObject("CameraProxy").transform;
    }

    private void Start()
    {
        Debug.Log(cam.Follow.name);
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

        float playerX = player.position.x;
        bool edgeLocked = false;

        if (currentRoom.LockX)
        {
            if (playerX >= currentRoom.MaxX) edgeLocked = true;
            if (playerX <= currentRoom.MinX) edgeLocked = true;
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

        if (forcingYOffset)
        {
            offset.y = Mathf.Lerp(offset.y, currentRoom.TrackedYOffset, 5f * Time.deltaTime);
            transposer.m_TrackedObjectOffset = offset;

            transposer.m_SoftZoneHeight = 2f;
            transposer.m_DeadZoneHeight = 0f;

            if (Mathf.Abs(offset.y - currentRoom.TrackedYOffset) > 0.05f) return;
            forcingYOffset = false;
        }

        offset.y = Mathf.Lerp(offset.y, currentRoom.TrackedYOffset, 5f * Time.deltaTime);
        transposer.m_TrackedObjectOffset = offset;

        float playerY = player.position.y;
        bool edgeLocked = false;

        if (currentRoom.LockY)
        {
            if (playerY >= currentRoom.MaxY) edgeLocked = true;
            if (playerY <= currentRoom.MinY) edgeLocked = true;
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
        if (currentRoom == room)
            return;


        currentRoom = room;
        forcingYOffset = currentRoom.ForceYOffset;
    }

    public void SnapAndHandover(CameraRoomBounds room)
    {
        SetRoom(room);

        StartCoroutine(HandoverRoutine());
    }

    private IEnumerator HandoverRoutine()
    {
        Vector3 clampedPos = player.position;

        if (currentRoom.LockX)
            clampedPos.x = Mathf.Clamp(clampedPos.x, currentRoom.MinX, currentRoom.MaxX);

        if (currentRoom.LockY)
            clampedPos.y = Mathf.Clamp(clampedPos.y, currentRoom.MinY, currentRoom.MaxY);

        proxyTarget.position = clampedPos;

        cam.Follow = proxyTarget;

            yield return null;

        cam.Follow = player;
    }
}
