using UnityEngine;

/// <summary>Опциональный предмет на уровне: при касании игроком сообщает LevelManager и исчезает.</summary>
public class ItemPickupTrigger : MonoBehaviour
{
    [Tooltip("Ссылка на LevelManager, чтобы уведомить об успешном подборе")]
    public LevelManager levelManager;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Ссылка может быть не назначена в инспекторе (например, предмет добавили на этаж позже) — ищем сами
        if (levelManager == null) levelManager = FindFirstObjectByType<LevelManager>();
        if (levelManager != null) levelManager.OnItemPickedUp();
        gameObject.SetActive(false);
    }
}
