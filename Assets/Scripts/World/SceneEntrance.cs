using UnityEngine;
using Cinemachine;
using System.Collections;

public class SceneEntrance : MonoBehaviour
{
    [Header("Entrance Settings")]
    [SerializeField] private string entranceId;
    [SerializeField] private CameraRoomBounds startingRoom;

    private IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();

        Debug.Log(
    "Entering: " + entranceId +
    " Room: " + startingRoom.name
);
        var run = GameManager.Instance.activeRun;

        if (run.isTransitioningScenes && run.targetEntranceId == entranceId)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            CameraManager camManager = FindObjectOfType<CameraManager>();

            if (player != null)
            {
                player.transform.position = transform.position;

                if (camManager != null && startingRoom != null)
                {
                    camManager.SnapAndHandover(startingRoom);
                }
            }

            run.isTransitioningScenes = false;
        }
    }
}