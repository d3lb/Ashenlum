using UnityEngine;

// Lives on Managers. Puts the shade back into the world every time the player loads into
// the scene they died in and has not collected it yet.
public class ShadeSpawner : MonoBehaviour
{
    [SerializeField] private PlayerShade shadePrefab;

    // Subscribed in Start rather than OnEnable: GameManager assigns its Instance in Awake
    // and both components sit on the same prefab, so Awake order between them is not
    // guaranteed.
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
