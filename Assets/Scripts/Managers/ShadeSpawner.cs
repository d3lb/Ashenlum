using UnityEngine;

// Puts the shade back whenever the player loads into the scene they died in.
public class ShadeSpawner : MonoBehaviour {
    [SerializeField] private PlayerShade shadePrefab;

    // The drop position is the player's pivot, which sits at his feet.
    [SerializeField] private float yOffset = 0.5f;

    // Start, not OnEnable: Awake order against GameManager is not guaranteed.
    private void Start() {
        if (GameManager.Instance != null)
            GameManager.Instance.OnSceneReady += SpawnIfOwed;
    }

    private void OnDestroy() {
        if (GameManager.Instance != null)
            GameManager.Instance.OnSceneReady -= SpawnIfOwed;
    }

    private void SpawnIfOwed() {
        GameRunProfile run = GameManager.Instance.activeRun;

        if (!run.HasShade)                    return;
        if (run.dropScene != run.currentArea) return;

        if (shadePrefab == null) {
            Debug.LogError("[ShadeSpawner] No shade prefab assigned - the player's lumens are unreachable.", this);
            return;
        }

        Vector3 at = run.dropPosition + Vector2.up * yOffset;
        Instantiate(shadePrefab, at, Quaternion.identity);
    }
}
