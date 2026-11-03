using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Тесты меняют статический GameState и пишут PlayerPrefs (ResetRun вызывает SaveData).
/// Песочница запоминает поля и сохранённые настройки до теста и возвращает их после,
/// чтобы прогон тестов не портил сохранение и настройки разработчика.
/// </summary>
public sealed class GameStateSandbox : System.IDisposable
{
    private static readonly FieldInfo[] Fields = CollectFields();

    private readonly Dictionary<FieldInfo, object> memory = Snapshot();
    private readonly Dictionary<FieldInfo, object> saved;

    public GameStateSandbox()
    {
        GameState.LoadData();
        saved = Snapshot();
        Restore(memory);
    }

    public void Dispose()
    {
        // Возвращаем сохранение: SaveData запишет те же значения, что были в PlayerPrefs до теста
        Restore(saved);
        GameState.SaveData();
        Restore(memory);
        PlayerStealth.SetInvisible(false);
    }

    private static FieldInfo[] CollectFields()
    {
        var result = new List<FieldInfo>();
        foreach (FieldInfo field in typeof(GameState).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        {
            if (!field.IsLiteral && !field.IsInitOnly) result.Add(field);
        }
        return result.ToArray();
    }

    private static Dictionary<FieldInfo, object> Snapshot()
    {
        var values = new Dictionary<FieldInfo, object>();
        foreach (FieldInfo field in Fields) values[field] = field.GetValue(null);
        return values;
    }

    private static void Restore(Dictionary<FieldInfo, object> values)
    {
        foreach (KeyValuePair<FieldInfo, object> pair in values) pair.Key.SetValue(null, pair.Value);
    }
}
