using UnityEngine;
using Cinemachine;

// Everything down, then one up. Setting only two priorities left any third camera holding a
// stale value, and equal priorities let Cinemachine pick whichever it likes.
public class CameraSwitcher : MonoBehaviour {
    [SerializeField] private CinemachineVirtualCamera gameplayCam;
    [SerializeField] private CinemachineVirtualCamera bossCam;

    // Secret rooms and anywhere else that wants its own framing. Any number.
    [SerializeField] private CinemachineVirtualCamera[] roomCams;

    private const int Live = 10;
    private const int Idle = 0;

    public void SwitchToBossCam() => SwitchTo(bossCam);

    public void SwitchToGameplayCam() => SwitchTo(gameplayCam);

    public void SwitchTo(CinemachineVirtualCamera cam) {
        Lower(gameplayCam);
        Lower(bossCam);

        if (roomCams != null)
            foreach (CinemachineVirtualCamera roomCam in roomCams) Lower(roomCam);

        if (cam != null) cam.Priority = Live;
    }

    private static void Lower(CinemachineVirtualCamera cam) {
        if (cam != null) cam.Priority = Idle;
    }
}
