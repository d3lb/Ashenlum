using UnityEngine;

// Runs before any scene loads, so the game works no matter which scene you pressed Play on.
public static class Bootstrapper {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Execute() {
        // Before the managers, and outside the early-out below: settings must apply on
        // every launch, not only the one where the managers happen to be created.
        GameSettings.Load();

        if (GameManager.Instance != null)
            return;

        GameObject managerPrefab = Resources.Load<GameObject>("Managers");

        if (managerPrefab != null) {
            Object.Instantiate(managerPrefab);
        }
        else {
            Debug.LogError("Could not find Managers prefab!");
        }
    }
}