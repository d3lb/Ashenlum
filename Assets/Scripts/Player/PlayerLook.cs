using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    private PlayerState state;
    private PlayerInput input;
    private CameraManager cameraManager;

    private float idleTimer;
    private float lookActivateDelay = 0.5f;

    private void Awake()
    {
        state = GetComponent<PlayerState>();
        input = GetComponent<PlayerInput>();
        cameraManager = FindFirstObjectByType<CameraManager>();
    }

    private void Update()
    {
        if (state.CurrentState != PlayerState.PlayerStateType.Idle)
        {
            idleTimer = 0f;
            cameraManager.ResetLook();
            return;
        }

        idleTimer += Time.deltaTime;

        if (idleTimer < lookActivateDelay)
            return;

        CameraRoomBounds room = cameraManager.CurrentRoom;

        if (room == null)
            return;

        if (input.LookUpHeld && room.AllowLookUp)
        {
            cameraManager.LookUp();
        }
        else if (input.LookDownHeld && room.AllowLookDown)
        {
            cameraManager.LookDown();
        }
        else
        {
            cameraManager.ResetLook();
        }
    }
}