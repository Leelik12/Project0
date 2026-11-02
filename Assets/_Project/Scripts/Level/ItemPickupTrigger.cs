using UnityEngine;

/// <summary>Опциональный предмет на уровне: при касании игроком сообщает LevelManager и исчезает.</summary>
public class ItemPickupTrigger : MonoBehaviour
{
    [Tooltip("Ссылка на LevelManager, чтобы уведомить об успешном подборе")]
    public LevelManager levelManager;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // В собранном этаже предмет приходит в префабе комнаты, ссылку на сцену ему не проставить заранее
        if (levelManager == null) levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager != null) levelManager.OnItemPickedUp();
        gameObject.SetActive(false);
    }
}
