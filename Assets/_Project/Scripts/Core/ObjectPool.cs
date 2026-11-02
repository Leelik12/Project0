using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Пул для короткоживущих эффектов (дым и искры попаданий, трассеры): вместо Instantiate/Destroy на каждый выстрел
/// объект выключается и переиспользуется. Пул сбрасывается при загрузке сцены — объекты сцены уничтожаются вместе с ней.
/// </summary>
public static class ObjectPool
{
    private static readonly Dictionary<GameObject, Stack<PooledObject>> Pools = new();

    /// <summary>Достаёт экземпляр prefab из пула (или создаёт) и возвращает его в пул через lifetime секунд.</summary>
    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
    {
        if (prefab == null) return null;

        if (!Pools.TryGetValue(prefab, out Stack<PooledObject> pool))
        {
            pool = new Stack<PooledObject>();
            Pools.Add(prefab, pool);
        }

        PooledObject item = null;
        while (pool.Count > 0 && item == null)
        {
            item = pool.Pop(); // уничтоженные объекты (например, эффект сам себя удалил) пропускаем
        }

        if (item == null)
        {
            GameObject instance = Object.Instantiate(prefab, position, rotation);
            item = instance.AddComponent<PooledObject>();
            item.Init(pool);
        }
        else
        {
            item.transform.SetPositionAndRotation(position, rotation);
        }

        item.Activate(lifetime);
        return item.gameObject;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        Pools.Clear();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) Pools.Clear();
    }
}
