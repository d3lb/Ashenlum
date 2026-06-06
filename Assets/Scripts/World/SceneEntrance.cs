using UnityEngine;
using Cinemachine;
using System.Collections;

public class SceneEntrance : MonoBehaviour
{
    [Header("Entrance Settings")]
    [SerializeField] private string entranceId;
    [SerializeField] private CameraRoomBounds startingRoom;

    public string EntranceId => entranceId;

    private void Start()
    {

        var run = GameManager.Instance.activeRun;

        if (!run.isTransitioningScenes)
            return;


        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        SceneEntrance[] entrances = FindObjectsByType<SceneEntrance>(FindObjectsSortMode.None);
        foreach (var entrance in entrances)
        {
            if (entrance.entranceId == run.targetEntranceId)
            {
                player.transform.position = entrance.transform.position;
                break;
            }
        }

        run.isTransitioningScenes = false;
        StartCoroutine(SceneFader.Instance.FadeIn(0.35f));
    }
}