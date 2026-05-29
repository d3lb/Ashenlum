using UnityEngine;

public class SceneEntrance : MonoBehaviour
{
    [Header("Entrance Settings")]
    [SerializeField] private string entranceId;

    private void Start()
    {
        var run = GameManager.Instance.activeRun;

        if (run.isTransitioningScenes && run.targetEntranceId == entranceId)
        {
            // Find the player in the new scene
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = transform.position;
            }

            // Turn off the transition state
            run.isTransitioningScenes = false;
        }
    }
}