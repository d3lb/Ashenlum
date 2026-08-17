using System.Collections.Generic;
using UnityEngine;

// Everything a rest puts back. Enemies register themselves in Awake, so this still knows
// about the ones that are switched off because they are dead - a search would not.
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
        // Enemies in scenes that are not loaded cannot be told anything. Emptying the set
        // is what brings them back - EnemyHealth.Start reads it when that scene loads.
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

        // Leftovers from kills that no longer happened. These are always active, so a
        // search finds them - only the dead enemies needed a registry to be findable.
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
