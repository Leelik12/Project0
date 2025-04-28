using UnityEngine;
using UnityEngine.UI; // Для работы с UI, если будет текст

public class LevelManager : MonoBehaviour
{
    [Header("Настройки уровня")]
    public GameObject[] enemies;      // Все враги на уровне
    public GameObject optionalItem;   // Опциональный предмет для подбора (может быть null)

    [Header("UI элементы")]
    public Text progressText;         // Текст прогресса (заполни в инспекторе)

    private bool itemPickedUp = false;
    private bool allEnemiesDefeated = false;

    void Start()
    {
        // Инициализация: можно обновить UI в начале
        UpdateProgressUI();
    }

    void Update()
    {
        CheckMissionStatus();
    }

    void CheckMissionStatus()
    {
        // Проверка всех врагов
        allEnemiesDefeated = AreAllEnemiesDefeated();

        // Проверка предмета
        if (optionalItem == null)
        {
            itemPickedUp = true; // Если предмета нет, считаем его сразу подобранным
        }

        // Если все условия выполнены
        if (allEnemiesDefeated && itemPickedUp)
        {
            LevelCompleted();
        }

        // Обновляем прогресс на UI
        UpdateProgressUI();
    }

    bool AreAllEnemiesDefeated()
    {
        foreach (GameObject enemy in enemies)
        {
            if (enemy != null) // Если враг еще существует
            {
                return false;
            }
        }
        return true;
    }

    public void OnItemPickedUp()
    {
        itemPickedUp = true;
    }

    void UpdateProgressUI()
    {
        if (progressText != null)
        {
            string progress = "";

            if (allEnemiesDefeated)
                progress += "Враги уничтожены! ";
            else
                progress += "Уничтожьте всех врагов. ";

            if (optionalItem != null)
            {
                if (itemPickedUp)
                    progress += "Предмет подобран!";
                else
                    progress += "Найдите предмет.";
            }

            progressText.text = progress;
        }
    }

    void LevelCompleted()
    {
        Debug.Log("Уровень пройден!");
        StaticHolder.level1CheksComplete = true;
        // Здесь вызови свой метод завершения уровня
        // Например: GameManager.Instance.CompleteLevel();
    }
}

