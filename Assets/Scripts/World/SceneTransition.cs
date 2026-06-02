using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    [Header("Transition Settings")]
    [SerializeField] private string sceneToLoad;
    [SerializeField] private string targetEntranceId;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            //  where we are going
            GameManager.Instance.activeRun.targetEntranceId = targetEntranceId;
            GameManager.Instance.activeRun.isTransitioningScenes = true;

            // save health to activeRun before leaving so they don't heal by walking through doors
            PlayerHealth hpScript = other.GetComponent<PlayerHealth>();
            if (hpScript != null)
            {
                GameManager.Instance.activeRun.currentHp = hpScript.CurrentHP;
            }

            // Load the new room
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}

