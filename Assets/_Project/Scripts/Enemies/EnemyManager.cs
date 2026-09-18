using System.Collections.Generic;
using UnityEngine;

/// <summary>Реестр живых врагов — нужен для «заражения» агрессией между соседями.</summary>
public static class EnemyManager
{
    public static readonly List<EnemyStateManager> AllEnemies = new();

    public static void Register(EnemyStateManager enemy)
    {
        if (!AllEnemies.Contains(enemy)) AllEnemies.Add(enemy);
    }

    public static void Unregister(EnemyStateManager enemy)
    {
        AllEnemies.Remove(enemy);
    }
}
