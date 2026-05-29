using UnityEngine;

public static class Bootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Execute()
    {
        if (GameManager.Instance != null)
            return;

        GameObject managerPrefab = Resources.Load<GameObject>("Managers");

        if (managerPrefab != null)
        {
            Object.Instantiate(managerPrefab);
        }
        else
        {
            Debug.LogError("Could not find Managers prefab!");
        }
    }
}