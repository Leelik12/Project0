using UnityEngine;

/// <summary>
/// После покупки апгрейда прячет все карточки (кроме исключений) и открывает лифт на следующий этаж.
/// Имя класса и метод DisableAllCanvasesExceptExcluded привязаны к кнопкам в сцене Lobby — не переименовывать.
/// </summary>
public class CanvasManager : MonoBehaviour
{
    public Canvas[] excludedCanvases; // канвасы, которые остаются видимыми (HUD, меню)

    public void DisableAllCanvasesExceptExcluded()
    {
        GameState.UpdateLevelEnd = true;

        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (System.Array.IndexOf(excludedCanvases, canvas) < 0)
            {
                canvas.gameObject.SetActive(false);
            }
        }
    }
}
