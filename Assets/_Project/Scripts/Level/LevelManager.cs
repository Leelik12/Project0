using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Цели этажа: уничтожить всех врагов (обязательно) и найти предмет (опционально).
/// При старте делает случайного врага элитным, за зачистку и предмет начисляет эдди.
/// </summary>
public class LevelManager : MonoBehaviour
{
    private const int MinEnemiesForElite = 3;

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
        MakeElites();
    }

    // Элита: один случайный враг этажа, на сложном — два. На маленьких этажах элиты нет.
    private void MakeElites()
    {
        var candidates = new List<EnemyHealth>();
        foreach (EnemyHealth health in healths)
        {
            if (health != null && !health.IsDead && health.TryGetComponent(out EnemyStateManager _)) candidates.Add(health);
        }
        if (candidates.Count < MinEnemiesForElite) return;

        int count = GameState.Difficulty ? 2 : 1;
        for (int i = 0; i < count && candidates.Count > 0; i++)
        {
            int index = Random.Range(0, candidates.Count);
            EliteEnemy.Make(candidates[index]);
            candidates.RemoveAt(index);
        }
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
            GameState.AddEddies(UpgradeCatalog.Instance.floorClearReward);
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
        if (!itemPickedUp) GameState.AddEddies(UpgradeCatalog.Instance.itemPickupReward);
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
