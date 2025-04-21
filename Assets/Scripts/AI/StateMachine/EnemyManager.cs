using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    public static List<EnemyStateManager> AllEnemies = new List<EnemyStateManager>();
    public static void Register(EnemyStateManager enemy)
    {
        if (!AllEnemies.Contains(enemy))
            AllEnemies.Add(enemy);
    }

    public static void Unregister(EnemyStateManager enemy)
    {
        if (AllEnemies.Contains(enemy))
            AllEnemies.Remove(enemy);
    }
}