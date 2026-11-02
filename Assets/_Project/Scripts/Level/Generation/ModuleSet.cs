using UnityEngine;

/// <summary>
/// Набор заготовок, из которых собирается этаж. Один ассет на этаж или на визуальную тему.
/// Чтобы какой-то модуль попадался чаще, добавь его в список несколько раз.
/// </summary>
[CreateAssetMenu(menuName = "Project0/Module Set", fileName = "ModuleSet")]
public class ModuleSet : ScriptableObject
{
    [Header("Комнаты")]
    [Tooltip("С чего начинается этаж. У комнаты должна быть точка playerStart.")]
    public RoomModule[] entryRooms;

    [Tooltip("Обычные комнаты между входом и выходом.")]
    public RoomModule[] rooms;

    [Tooltip("Комнаты с лифтом на следующий этаж. Ставятся в самый дальний от входа конец.")]
    public RoomModule[] exitRooms;

    [Tooltip("Тупиковые комнаты с опциональным предметом (ItemPickupTrigger внутри).")]
    public RoomModule[] itemRooms;

    [Header("Коридоры")]
    [Tooltip("Связывают комнаты между собой. Если список пуст — комнаты стыкуются напрямую.")]
    public RoomModule[] corridors;

    [Header("Прочее")]
    [Tooltip("Заглушка для проёма, к которому ничего не пристыковалось. Ставится в точку проёма, её +Z смотрит наружу.")]
    public GameObject connectorCap;

    [Tooltip("Враги по умолчанию — для точек спавна без собственного списка.")]
    public GameObject[] enemyPrefabs;
}
