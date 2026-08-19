using UnityEngine;

// Puts the shade back whenever the player loads into the scene they died in.
public class ShadeSpawner : MonoBehaviour
{
    [SerializeField] private PlayerShade shadePrefab;

    // Start, not OnEnable: Awake order against GameManager is not guaranteed.
    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnSceneReady += SpawnIfOwed;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnSceneReady -= SpawnIfOwed;
    }

    private void SpawnIfOwed()
    {
        GameRunProfile run = GameManager.Instance.activeRun;

        if (!run.HasShade)                    return;
        if (run.dropScene != run.currentArea) return;

        if (shadePrefab == null)
        {
            Debug.LogError("[ShadeSpawner] No shade prefab assigned - the player's lumens are unreachable.", this);
            return;
        }

        Instantiate(shadePrefab, run.dropPosition, Quaternion.identity);
    }
}
