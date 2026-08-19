using System.Collections.Generic;
using UnityEngine;

// Enemies register themselves - a dead one is disabled and a search cannot find it.
public static class WorldReset
{
    private static readonly List<EnemyHealth> enemies = new();

    public static void Register(EnemyHealth enemy)
    {
        if (enemy != null && !enemies.Contains(enemy)) enemies.Add(enemy);
    }

    public static void Unregister(EnemyHealth enemy) => enemies.Remove(enemy);

    public static void ResetAll()
    {
        // Unloaded scenes cannot be told anything; emptying the set is what revives them.
        GameRunProfile run = GameManager.Instance != null ? GameManager.Instance.activeRun : null;
        run?.temporaryRemoved.Clear();

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            EnemyHealth enemy = enemies[i];

            if (enemy == null)
            {
                enemies.RemoveAt(i);
                continue;
            }

            enemy.ResetToSpawn();
        }

        // Always active, so a search finds these - only dead enemies needed the registry.
        Sweep<Corpse>();
        Sweep<LumenPickup>(lumen => !lumen.IsFlying);
    }

    private static void Sweep<T>(System.Func<T, bool> shouldRemove = null) where T : Component
    {
        foreach (T item in Object.FindObjectsByType<T>(FindObjectsSortMode.None))
            if (shouldRemove == null || shouldRemove(item))
                Object.Destroy(item.gameObject);
    }
}
