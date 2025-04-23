using UnityEngine;

public class ActivateOnTrigger : MonoBehaviour
{
    [SerializeField] EnemyStateManager[] enemyStateManagers;
    void Start()
    {
        foreach (EnemyStateManager manager in enemyStateManagers)
        {
            manager.enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"{other.name} вошёл");
        if (other.gameObject.CompareTag("Player"))
        {
            Debug.Log("Игрок вошёл");
            foreach (EnemyStateManager manager in enemyStateManagers)
            {
                Debug.Log($"{manager.name} активировался");
                manager.enabled = true;
            }
        }
    }
}
