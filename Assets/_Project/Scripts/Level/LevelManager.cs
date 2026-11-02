using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Цели этажа: уничтожить всех врагов (обязательно) и найти предмет (опционально).</summary>
public class LevelManager : MonoBehaviour
{
    [Header("Настройки уровня")]
    public List<GameObject> enemies = new(); // все враги на уровне; сборщик этажа дописывает сюда заспавненных
    public GameObject optionalItem;   // опциональный предмет (может быть null)

    [Header("UI элементы")]
    public Text progressText;

    private bool itemPickedUp;
    private bool levelCompleted;

    private void Start()
    {
        UpdateProgressUI();
    }

    private void Update()
    {
        if (levelCompleted) return;

        if (AreAllEnemiesDefeated())
        {
            levelCompleted = true;
            GameState.levelCheksComplete = true;
            Debug.Log("Уровень пройден");
            UpdateProgressUI();
        }
    }

    private bool AreAllEnemiesDefeated()
    {
        foreach (GameObject enemy in enemies)
        {
            if (enemy != null) return false; // уничтоженные враги становятся null
        }
        return true;
    }

    /// <summary>Добавляет врага, появившегося в рантайме (собранные этажи).</summary>
    public void RegisterEnemy(GameObject enemy)
    {
        if (enemy != null && !enemies.Contains(enemy)) enemies.Add(enemy);
    }

    public void OnItemPickedUp()
    {
        itemPickedUp = true;
        GameState.ItemPickedUp = true;
        UpdateProgressUI();
    }

    private void UpdateProgressUI()
    {
        if (progressText == null) return;

        string progress = levelCompleted ? "Враги уничтожены! " : "Уничтожьте всех врагов. ";
        if (optionalItem != null)
        {
            progress += itemPickedUp ? "Предмет подобран!" : "Найдите предмет.";
        }
        progressText.text = progress;
    }
}
