using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    private CameraManager cameraManager;
    private PlayerState state;

    private CameraRoomBounds currentRoom;

    private void Awake()
    {
        cameraManager = FindFirstObjectByType<CameraManager>();
        state = GetComponent<PlayerState>();
    }

    private void Update()
    {
        if (state.CurrentState != PlayerState.PlayerStateType.Idle)
            return;

        if (Input.GetButtonDown("AimUp") && currentRoom != null && currentRoom.allowLookUp)
        {
            cameraManager.LookUp();
        }

        if (Input.GetButtonDown("AimDown") && currentRoom != null && currentRoom.allowLookDown)
        {
            cameraManager.LookDown();
        }
    }

    public void SetCurrentRoom(CameraRoomBounds room)
    {
        currentRoom = room;
    }
}