using UnityEngine;

/// <summary>Держит указанных врагов выключенными, пока игрок не войдёт в триггер (засада).</summary>
public class ActivateOnTrigger : MonoBehaviour
{
    [SerializeField] private EnemyStateManager[] enemyStateManagers;

    private void Start()
    {
        foreach (EnemyStateManager manager in enemyStateManagers)
        {
            if (manager != null) manager.enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        foreach (EnemyStateManager manager in enemyStateManagers)
        {
            if (manager != null) manager.enabled = true;
        }
        enabled = false;
    }
}
