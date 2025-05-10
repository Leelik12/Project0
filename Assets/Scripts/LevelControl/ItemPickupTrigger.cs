using UnityEngine;

public class ItemPickupTrigger : MonoBehaviour
{
    [Tooltip("Ссылка на LevelManager, чтобы уведомить об успешном подборе")]
    public LevelManager levelManager;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Уведомляем LevelManager
            if (levelManager != null)
            {
                levelManager.OnItemPickedUp();
            }

            // Делаем предмет невидимым и неактивным
            gameObject.SetActive(false);
        }
    }
}
