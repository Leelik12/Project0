/// <summary>
/// Подготовка текста для киберпанк-шрифта интерфейса: пробел в нём очень узкий (втрое уже буквы),
/// поэтому все надписи в проекте набраны с двойными пробелами. Тексты из кода прогоняются через Widen.
/// </summary>
public static class UiText
{
    public static string Widen(string text) => string.IsNullOrEmpty(text) ? text : text.Replace("  ", " ").Replace(" ", "  ");
}
