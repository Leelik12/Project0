using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

/// <summary>
/// Собирает этаж в рантайме из заготовок <see cref="ModuleSet"/>: входная комната, затем
/// коридоры и комнаты вперемешку, комната с лифтом в самом дальнем конце и, если повезёт,
/// тупик с предметом. Пустые проёмы закрываются заглушками, после чего строится NavMesh
/// и расставляются враги.
///
/// Сначала весь этаж раскладывается «на бумаге» по данным префабов, и только удачная
/// раскладка превращается в объекты. Если выход пристыковать не удалось, пробуем заново
/// со следующим зерном.
/// </summary>
public class FloorAssembler : MonoBehaviour
{
    [SerializeField] private ModuleSet moduleSet;

    [Header("Размер этажа")]
    [Tooltip("Сколько обычных комнат будет между входом и выходом (случайно в этих пределах).")]
    [SerializeField] private int minRooms = 4;
    [SerializeField] private int maxRooms = 7;

    [Tooltip("Шанс, что на этаже появится тупик с предметом.")]
    [Range(0f, 1f)]
    [SerializeField] private float itemRoomChance = 0.7f;

    [Header("Генерация")]
    [Tooltip("0 — новый этаж при каждом запуске. Другое число — всегда один и тот же этаж.")]
    [SerializeField] private int seed = 0;

    [Tooltip("Сколько раз пробовать заново, если выход не удалось пристыковать.")]
    [SerializeField] private int maxAttempts = 20;

    [Tooltip("Насколько модули могут заходить друг в друга, в метрах. " +
             "Нужен, чтобы соприкасающиеся стены не считались пересечением.")]
    [SerializeField] private float overlapTolerance = 0.1f;

    [Header("Связи со сценой")]
    [Tooltip("Пусто — берётся NavMeshSurface с этого же объекта.")]
    [SerializeField] private NavMeshSurface navMeshSurface;

    [Tooltip("Пусто — ищется в сцене.")]
    [SerializeField] private LevelManager levelManager;

    [Tooltip("XR Origin. Если задан — переносится в точку старта входной комнаты.")]
    [SerializeField] private Transform playerRig;

    [SerializeField] private bool buildOnAwake = true;

    /// <summary>Зерно, которым собран текущий этаж. Подставь его в seed, чтобы получить этот же этаж.</summary>
    public int Seed { get; private set; }

    public IReadOnlyList<RoomModule> Modules => modules;
    public IReadOnlyList<GameObject> Enemies => enemies;

    // Данные префаба, нужные для раскладки. Снимаются один раз и без создания экземпляра.
    private class ModuleInfo
    {
        public Vector3[] connectorPositions;
        public Vector3[] connectorForwards;
        public int[] connectorSizes;
        public Bounds bounds;
    }

    // Модуль, поставленный в план этажа.
    private class Placement
    {
        public RoomModule prefab;
        public ModuleInfo info;
        public Vector3 position;
        public Quaternion rotation;
        public Bounds worldBounds;
        public int depth;               // сколько модулей до входа
        public int parent = -1;
        public int parentConnector = -1; // к какому проёму родителя пристыкован
        public int usedConnector = -1;   // каким своим проёмом пристыкован
        public bool removed;
        public bool isEntry;
        public bool isItem;
    }

    // Свободный проём поставленного модуля.
    private class Socket
    {
        public int owner;
        public int connector;
        public Vector3 position;
        public Vector3 forward;
        public int size;
    }

    private readonly List<RoomModule> modules = new();
    private readonly List<GameObject> enemies = new();
    private readonly Dictionary<RoomModule, ModuleInfo> infoCache = new();
    private readonly List<Placement> placed = new();
    private readonly List<Socket> open = new();

    private System.Random rng;
    private Transform root;

    private void Awake()
    {
        // В Awake, а не в Start: враги должны оказаться в LevelManager раньше, чем он начнёт их считать
        if (buildOnAwake) Build();
    }

    /// <summary>Собирает этаж заново.</summary>
    public void Build()
    {
        Clear();
        if (!IsModuleSetValid()) return;

        int baseSeed = seed != 0 ? seed : System.Environment.TickCount;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Seed = baseSeed + attempt;
            rng = new System.Random(Seed);

            if (!TryPlan()) continue;

            Spawn();
            return;
        }

        Debug.LogError($"{nameof(FloorAssembler)}: не удалось собрать этаж за {maxAttempts} попыток. " +
                       "Скорее всего, комнате выхода некуда встать — проверь габариты и проёмы модулей.", this);
    }

    /// <summary>Удаляет собранный этаж вместе с врагами.</summary>
    public void Clear()
    {
        modules.Clear();
        enemies.Clear();
        placed.Clear();
        open.Clear();

        Transform existing = transform.Find("AssembledFloor");
        if (existing == null) return;

        if (Application.isPlaying) Destroy(existing.gameObject);
        else DestroyImmediate(existing.gameObject);
    }

    [ContextMenu("Пересобрать этаж")]
    private void RebuildFromMenu()
    {
        if (Application.isPlaying) Build();
        else Debug.Log($"{nameof(FloorAssembler)}: пересборка работает только в Play Mode.", this);
    }

    private bool IsModuleSetValid()
    {
        if (moduleSet == null)
        {
            Debug.LogError($"{nameof(FloorAssembler)}: не задан ModuleSet.", this);
            return false;
        }
        if (IsEmpty(moduleSet.entryRooms) || IsEmpty(moduleSet.exitRooms))
        {
            Debug.LogError($"{nameof(FloorAssembler)}: в ModuleSet нужна хотя бы одна входная и одна выходная комната.", this);
            return false;
        }
        return true;
    }

    // ---------- Раскладка ----------

    private bool TryPlan()
    {
        placed.Clear();
        open.Clear();

        // Вход — в начало координат сборщика
        RoomModule entryPrefab = Pick(moduleSet.entryRooms);
        ModuleInfo entryInfo = Info(entryPrefab);
        Commit(new Placement
        {
            prefab = entryPrefab,
            info = entryInfo,
            position = Vector3.zero,
            rotation = Quaternion.identity,
            worldBounds = ToWorld(entryInfo.bounds, Vector3.zero, Quaternion.identity),
            isEntry = true
        });

        // Обычные комнаты: растим этаж от случайных свободных проёмов
        int targetRooms = rng.Next(Mathf.Min(minRooms, maxRooms), Mathf.Max(minRooms, maxRooms) + 1);
        int roomsPlaced = 0;
        if (!IsEmpty(moduleSet.rooms))
        {
            while (roomsPlaced < targetRooms && open.Count > 0)
            {
                Socket socket = open[rng.Next(open.Count)];
                open.Remove(socket);
                if (TryAttachRoom(socket, moduleSet.rooms, out _)) roomsPlaced++;
            }
        }

        // Выход — как можно дальше от входа, чтобы этаж пришлось пройти целиком
        if (!AttachToBestSocket(moduleSet.exitRooms, deepestFirst: true, out _)) return false;

        // Предмет — в любой свободный проём; не встал, значит на этом этаже его нет
        if (!IsEmpty(moduleSet.itemRooms) && rng.NextDouble() < itemRoomChance)
        {
            if (AttachToBestSocket(moduleSet.itemRooms, deepestFirst: false, out int itemIndex))
            {
                placed[itemIndex].isItem = true;
            }
        }

        PruneDeadEndCorridors();
        return true;
    }

    private bool AttachToBestSocket(RoomModule[] pool, bool deepestFirst, out int placedIndex)
    {
        var candidates = new List<Socket>(open);
        if (deepestFirst) candidates.Sort((a, b) => placed[b.owner].depth.CompareTo(placed[a.owner].depth));
        else Shuffle(candidates);

        foreach (Socket socket in candidates)
        {
            open.Remove(socket);
            if (TryAttachRoom(socket, pool, out placedIndex)) return true;
            open.Add(socket); // проём ещё может пригодиться следующему
        }

        placedIndex = -1;
        return false;
    }

    // Комнаты чередуются с коридорами: от коридора ставим комнату сразу, от комнаты — через коридор
    private bool TryAttachRoom(Socket socket, RoomModule[] pool, out int placedIndex)
    {
        bool fromCorridor = placed[socket.owner].prefab.kind == ModuleKind.Corridor;
        if (fromCorridor || IsEmpty(moduleSet.corridors)) return TryAttach(socket, pool, out placedIndex);

        foreach (Placement corridor in Fits(socket, moduleSet.corridors))
        {
            int corridorIndex = Commit(corridor);

            List<Socket> corridorExits = open.FindAll(s => s.owner == corridorIndex);
            Shuffle(corridorExits);

            foreach (Socket exit in corridorExits)
            {
                open.Remove(exit);
                if (TryAttach(exit, pool, out placedIndex)) return true;
                open.Add(exit);
            }

            Rollback(corridorIndex); // за коридором комната не помещается — убираем и коридор
        }

        placedIndex = -1;
        return false;
    }

    private bool TryAttach(Socket socket, RoomModule[] pool, out int placedIndex)
    {
        foreach (Placement placement in Fits(socket, pool))
        {
            placedIndex = Commit(placement);
            return true;
        }

        placedIndex = -1;
        return false;
    }

    // Перебирает все положения модулей из пула, которые встают в проём и ни с чем не пересекаются
    private IEnumerable<Placement> Fits(Socket socket, RoomModule[] pool)
    {
        foreach (RoomModule prefab in Shuffled(pool))
        {
            if (prefab == null) continue;
            ModuleInfo info = Info(prefab);

            foreach (int connector in ShuffledIndices(info.connectorPositions.Length))
            {
                if (info.connectorSizes[connector] != socket.size) continue;

                // Поворачиваем модуль так, чтобы его проём смотрел навстречу свободному проёму
                float yaw = Yaw(-socket.forward) - Yaw(info.connectorForwards[connector]);
                Quaternion rotation = Quaternion.Euler(0f, Mathf.Round(yaw / 90f) * 90f, 0f);
                Vector3 position = socket.position - rotation * info.connectorPositions[connector];

                Bounds worldBounds = ToWorld(info.bounds, position, rotation);
                if (Overlaps(worldBounds)) continue;

                yield return new Placement
                {
                    prefab = prefab,
                    info = info,
                    position = position,
                    rotation = rotation,
                    worldBounds = worldBounds,
                    depth = placed[socket.owner].depth + 1,
                    parent = socket.owner,
                    parentConnector = socket.connector,
                    usedConnector = connector
                };
            }
        }
    }

    private int Commit(Placement placement)
    {
        int index = placed.Count;
        placed.Add(placement);

        ModuleInfo info = placement.info;
        for (int i = 0; i < info.connectorPositions.Length; i++)
        {
            if (i == placement.usedConnector) continue;
            open.Add(new Socket
            {
                owner = index,
                connector = i,
                position = placement.position + placement.rotation * info.connectorPositions[i],
                forward = placement.rotation * info.connectorForwards[i],
                size = info.connectorSizes[i]
            });
        }

        return index;
    }

    // Откатывает последний поставленный модуль вместе с его свободными проёмами
    private void Rollback(int index)
    {
        open.RemoveAll(s => s.owner == index);
        placed.RemoveAt(index);
    }

    private bool Overlaps(Bounds candidate)
    {
        candidate.Expand(-2f * overlapTolerance);
        foreach (Placement other in placed)
        {
            if (!other.removed && candidate.Intersects(other.worldBounds)) return true;
        }
        return false;
    }

    // Коридор, за которым так ничего и не встало, — просто аппендикс. Убираем, проём закроет заглушка.
    private void PruneDeadEndCorridors()
    {
        bool changed = true;
        while (changed)
        {
            changed = false;
            for (int i = 0; i < placed.Count; i++)
            {
                Placement placement = placed[i];
                if (placement.removed || placement.isEntry || placement.prefab.kind != ModuleKind.Corridor) continue;
                if (HasChildren(i)) continue;

                placement.removed = true;
                changed = true;
            }
        }
    }

    private bool HasChildren(int index)
    {
        foreach (Placement placement in placed)
        {
            if (!placement.removed && placement.parent == index) return true;
        }
        return false;
    }

    private bool IsConnectorUsed(int index, int connector)
    {
        Placement placement = placed[index];
        if (placement.parent >= 0 && connector == placement.usedConnector) return true;

        foreach (Placement child in placed)
        {
            if (!child.removed && child.parent == index && child.parentConnector == connector) return true;
        }
        return false;
    }

    // ---------- Создание объектов ----------

    private void Spawn()
    {
        root = new GameObject("AssembledFloor").transform;
        root.SetParent(transform, worldPositionStays: false);

        RoomModule entry = null;
        RoomModule itemRoom = null;

        for (int i = 0; i < placed.Count; i++)
        {
            Placement placement = placed[i];
            if (placement.removed) continue;

            GameObject instance = Instantiate(placement.prefab.gameObject, root);
            instance.name = $"{i:00}_{placement.prefab.name}";
            instance.transform.SetLocalPositionAndRotation(placement.position, placement.rotation);

            RoomModule module = instance.GetComponent<RoomModule>();
            modules.Add(module);
            if (placement.isEntry) entry = module;
            if (placement.isItem) itemRoom = module;

            SpawnCaps(i, placement);
        }

        BuildNavMesh();
        SpawnEnemies(entry);
        LinkItem(itemRoom);
        MovePlayer(entry);
    }

    private void SpawnCaps(int index, Placement placement)
    {
        if (moduleSet.connectorCap == null) return;

        ModuleInfo info = placement.info;
        for (int connector = 0; connector < info.connectorPositions.Length; connector++)
        {
            if (IsConnectorUsed(index, connector)) continue;

            Vector3 position = placement.position + placement.rotation * info.connectorPositions[connector];
            Vector3 forward = placement.rotation * info.connectorForwards[connector];

            GameObject cap = Instantiate(moduleSet.connectorCap, root);
            cap.transform.SetLocalPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
        }
    }

    private void BuildNavMesh()
    {
        if (navMeshSurface == null) navMeshSurface = GetComponent<NavMeshSurface>();
        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
        }
        else
        {
            Debug.LogWarning($"{nameof(FloorAssembler)}: нет NavMeshSurface — враги не смогут ходить.", this);
        }
    }

    private void SpawnEnemies(RoomModule entry)
    {
        if (levelManager == null) levelManager = FindFirstObjectByType<LevelManager>();

        Transform enemyRoot = new GameObject("Enemies").transform;
        enemyRoot.SetParent(root, worldPositionStays: false);

        foreach (RoomModule module in modules)
        {
            if (module == entry) continue; // во входной комнате врагов не ставим

            foreach (EnemySpawnPoint point in module.SpawnPoints)
            {
                GameObject[] pool = !IsEmpty(point.enemyPrefabs) ? point.enemyPrefabs : moduleSet.enemyPrefabs;
                if (IsEmpty(pool)) continue;

                GameObject prefab = pool[rng.Next(pool.Length)];
                if (prefab == null) continue;

                GameObject enemy = Instantiate(prefab, point.transform.position, point.transform.rotation, enemyRoot);

                EnemyStateManager stateManager = enemy.GetComponent<EnemyStateManager>();
                if (stateManager != null && !IsEmpty(point.patrolPoints)) stateManager.patrolPoints = point.patrolPoints;

                enemies.Add(enemy);
                if (levelManager != null) levelManager.RegisterEnemy(enemy);
            }
        }

        if (enemies.Count == 0)
        {
            Debug.LogWarning($"{nameof(FloorAssembler)}: на этаже ни одного врага — он засчитается пройденным сразу. " +
                             "Добавь EnemySpawnPoint в модули или врагов в ModuleSet.", this);
        }
    }

    private void LinkItem(RoomModule itemRoom)
    {
        if (itemRoom == null || levelManager == null) return;

        ItemPickupTrigger item = itemRoom.GetComponentInChildren<ItemPickupTrigger>(true);
        if (item == null) return;

        item.levelManager = levelManager;
        levelManager.optionalItem = item.gameObject;
    }

    private void MovePlayer(RoomModule entry)
    {
        if (playerRig == null || entry == null || entry.playerStart == null) return;

        // CharacterController перезаписывает позицию, пока включён
        CharacterController controller = playerRig.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;

        Transform start = entry.playerStart;
        playerRig.SetPositionAndRotation(start.position, Quaternion.Euler(0f, start.eulerAngles.y, 0f));

        if (controller != null) controller.enabled = true;
    }

    // ---------- Вспомогательное ----------

    private ModuleInfo Info(RoomModule prefab)
    {
        if (infoCache.TryGetValue(prefab, out ModuleInfo cached)) return cached;

        ModuleConnector[] connectors = prefab.Connectors;
        var info = new ModuleInfo
        {
            connectorPositions = new Vector3[connectors.Length],
            connectorForwards = new Vector3[connectors.Length],
            connectorSizes = new int[connectors.Length],
            bounds = prefab.LocalBounds
        };

        for (int i = 0; i < connectors.Length; i++)
        {
            Transform connector = connectors[i].transform;
            Vector3 forward = prefab.transform.InverseTransformDirection(connector.forward);
            forward.y = 0f;

            info.connectorPositions[i] = prefab.transform.InverseTransformPoint(connector.position);
            info.connectorForwards[i] = forward.normalized;
            info.connectorSizes[i] = connectors[i].size;
        }

        if (connectors.Length == 0)
        {
            Debug.LogWarning($"{nameof(FloorAssembler)}: у модуля {prefab.name} нет ни одного ModuleConnector — к нему нечем пристыковаться.", prefab);
        }

        infoCache[prefab] = info;
        return info;
    }

    private static Bounds ToWorld(Bounds local, Vector3 position, Quaternion rotation)
    {
        Vector3 center = position + rotation * local.center;
        Bounds result = new Bounds(center, Vector3.zero);

        for (int corner = 0; corner < 8; corner++)
        {
            Vector3 offset = new Vector3(
                (corner & 1) == 0 ? -local.extents.x : local.extents.x,
                (corner & 2) == 0 ? -local.extents.y : local.extents.y,
                (corner & 4) == 0 ? -local.extents.z : local.extents.z);
            result.Encapsulate(center + rotation * offset);
        }

        return result;
    }

    private static float Yaw(Vector3 direction) => Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

    private static bool IsEmpty<T>(T[] array) => array == null || array.Length == 0;

    private T Pick<T>(T[] array) where T : Object
    {
        var valid = new List<T>();
        foreach (T item in array)
        {
            if (item != null) valid.Add(item);
        }
        return valid.Count > 0 ? valid[rng.Next(valid.Count)] : null;
    }

    private List<T> Shuffled<T>(T[] array)
    {
        var list = new List<T>(array ?? new T[0]);
        Shuffle(list);
        return list;
    }

    private List<int> ShuffledIndices(int count)
    {
        var list = new List<int>(count);
        for (int i = 0; i < count; i++) list.Add(i);
        Shuffle(list);
        return list;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        foreach (Placement placement in placed)
        {
            if (placement.removed) continue;
            Gizmos.color = placement.isEntry ? Color.green : placement.prefab.kind == ModuleKind.Room ? Color.white : Color.gray;
            Gizmos.DrawWireCube(placement.worldBounds.center, placement.worldBounds.size);
        }
    }
}
