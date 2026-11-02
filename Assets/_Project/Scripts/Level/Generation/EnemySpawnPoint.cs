using UnityEngine;

/// <summary>
/// Точка появления врага внутри модуля. Ставится на уровне пола.
/// Сам враг создаётся сборщиком этажа уже после постройки NavMesh, иначе агент не встанет на сетку.
/// </summary>
public class EnemySpawnPoint : MonoBehaviour
{
    [Tooltip("Кого можно поставить в эту точку. Пусто — случайный враг из общего списка ModuleSet.")]
    public GameObject[] enemyPrefabs;

    [Tooltip("Маршрут патруля (дочерние объекты модуля). Пусто — враг стоит на месте, пока не заметит игрока.")]
    public Transform[] patrolPoints;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position + Vector3.up, 0.4f);
        Gizmos.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + transform.forward * 0.8f);

        if (patrolPoints == null) return;

        Vector3 previous = transform.position;
        foreach (Transform point in patrolPoints)
        {
            if (point == null) continue;
            Gizmos.DrawLine(previous, point.position);
            Gizmos.DrawWireCube(point.position, Vector3.one * 0.3f);
            previous = point.position;
        }
    }
}
