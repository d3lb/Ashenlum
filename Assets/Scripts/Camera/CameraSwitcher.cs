using UnityEngine;
using Cinemachine;

public class CameraSwitcher : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera gameplayCam;
    [SerializeField] private CinemachineVirtualCamera bossCam;

    public void SwitchToBossCam()
    {

        gameplayCam.Priority = 0;
        bossCam.Priority = 10;
    }

    public void SwitchToGameplayCam()
    {

        gameplayCam.Priority = 10;
        bossCam.Priority = 0;
    }
}