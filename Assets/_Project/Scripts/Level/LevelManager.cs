using UnityEngine;
using UnityEngine.UI;

/// <summary>Цели этажа: уничтожить всех врагов (обязательно) и найти предмет (опционально).</summary>
public class LevelManager : MonoBehaviour
{
    /// <summary>Сколько врагов на этаже ещё живы (для HUD). 0 — этаж зачищен или менеджера нет.</summary>
    public static int EnemiesRemaining { get; private set; }

    [Header("Настройки уровня")]
    public GameObject[] enemies;      // все враги на уровне
    public GameObject optionalItem;   // опциональный предмет (может быть null)

    [Header("UI элементы")]
    public Text progressText;

    private bool itemPickedUp;
    private bool levelCompleted;
    private EnemyHealth[] healths; // враг считается побеждённым сразу при смерти, не дожидаясь удаления трупа
    private int shownRemaining = -1;

    private void Start()
    {
        healths = new EnemyHealth[enemies.Length];
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null) healths[i] = enemies[i].GetComponentInChildren<EnemyHealth>();
        }
        EnemiesRemaining = CountAlive();
        UpdateProgressUI();
    }

    private void OnDestroy()
    {
        EnemiesRemaining = 0;
    }

    private void Update()
    {
        if (levelCompleted) return;

        EnemiesRemaining = CountAlive();
        if (EnemiesRemaining != shownRemaining) UpdateProgressUI();

        if (EnemiesRemaining == 0)
        {
            levelCompleted = true;
            GameState.LevelCleared = true;
            UpdateProgressUI();
        }
    }

    private int CountAlive()
    {
        int alive = 0;
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null) continue; // уничтоженные враги становятся null
            if (healths[i] != null && healths[i].IsDead) continue;
            alive++;
        }
        return alive;
    }

    public void OnItemPickedUp()
    {
        itemPickedUp = true;
        GameState.ItemPickedUp = true;
        UpdateProgressUI();
    }

    private void UpdateProgressUI()
    {
        shownRemaining = EnemiesRemaining;
        if (progressText == null) return;

        string progress = levelCompleted ? "Враги уничтожены! " : $"Уничтожьте всех врагов (осталось: {EnemiesRemaining}). ";
        if (optionalItem != null)
        {
            progress += itemPickedUp ? "Предмет подобран!" : "Найдите предмет.";
        }
        progressText.text = progress;
    }
}
