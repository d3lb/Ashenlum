using UnityEngine;
using Cinemachine;

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
        if (currentRoom == null)
            return;

        // target lookahead based on facing direction
        float desiredOffsetX =
            state.IsFacingRight ? howFar : -howFar;

        float tempFlipSpeed =
            state.IsGrounded ? flipSpeed : flipSpeed / 2f;

        Vector3 offset = transposer.m_TrackedObjectOffset;

        // smooth turning
        offset.x = Mathf.Lerp(
            offset.x,
            desiredOffsetX,
            tempFlipSpeed * Time.deltaTime
        );

        // current camera position
        float playerX = player.position.x;
        bool edgeLocked = false;

        if (currentRoom.LockX)
        {
            // right edge
            if (playerX >= currentRoom.MaxX)
            {
                edgeLocked = true;
            }

            // left edge
            if (playerX <= currentRoom.MinX)
            {
                edgeLocked = true;
            }
        }

        // camera behavior at edges
        if (edgeLocked)
        {
            transposer.m_SoftZoneWidth = 2f;
            transposer.m_DeadZoneWidth = 2f;
        }
        else
        {
            transposer.m_SoftZoneWidth = Mathf.Lerp(
                transposer.m_SoftZoneWidth,
                0.2f,
                7f * Time.deltaTime
            );

            transposer.m_DeadZoneWidth = Mathf.Lerp(
                transposer.m_DeadZoneWidth,
                0f,
                7f * Time.deltaTime
            );
        }

        transposer.m_TrackedObjectOffset = offset;
    }


    private void yAxis()
    {
        if (currentRoom == null)
            return;

        Vector3 offset = transposer.m_TrackedObjectOffset;

        // FORCE Y OFFSET TRANSITION
        if (forcingYOffset)
        {
            offset.y = Mathf.Lerp(
                offset.y,
                currentRoom.TrackedYOffset,
                5f * Time.deltaTime
            );

            transposer.m_TrackedObjectOffset = offset;

           
            transposer.m_SoftZoneHeight = 2f;
            transposer.m_DeadZoneHeight = 0f;

            // wait until close enough
            if (Mathf.Abs(offset.y - currentRoom.TrackedYOffset) > 0.05f)
            {
                return;
            }

            forcingYOffset = false;
        }

        // Y Tracked Object Offset
        offset.y = Mathf.Lerp(
            offset.y,
            currentRoom.TrackedYOffset,
            5f * Time.deltaTime
        );
        transposer.m_TrackedObjectOffset = offset;

        // normal jump/fall damping

        float playerY = player.position.y;

        bool edgeLocked = false;

        if (currentRoom.LockY)
        {
            // top edge
            if (playerY >= currentRoom.MaxY)
            {
                edgeLocked = true;
            }

            // bottom edge
            if (playerY <= currentRoom.MinY)
            {
                edgeLocked = true;
            }
        }

        // vertical edge behavior
        if (edgeLocked)
        {
            transposer.m_SoftZoneHeight = 2f;
            transposer.m_DeadZoneHeight = 2f;
        }
        else
        {
            transposer.m_SoftZoneHeight = Mathf.Lerp(
                transposer.m_SoftZoneHeight,
                0.5f,
                7f * Time.deltaTime
            );

            transposer.m_DeadZoneHeight = Mathf.Lerp(
                transposer.m_DeadZoneHeight,
                0f,
                7f * Time.deltaTime
            );
        }
    }



    // ROOM MANAGED SETTINGS    
    public void SetRoom(CameraRoomBounds room)
    {
        if (currentRoom != room)
        {
            currentRoom = room;
            forcingYOffset = currentRoom.ForceYOffset;
        }
    }
}
