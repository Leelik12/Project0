using UnityEngine;

public class CanvasManager : MonoBehaviour
{
    public Canvas[] excludedCanvases; // Укажи тут те 2 Canvas, которые нельзя отключать

    public void DisableAllCanvasesExceptExcluded()
    {
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        StaticHolder.UpdateLevelEnd = true;
        foreach (Canvas canvas in allCanvases)
        {
            bool isExcluded = false;

            foreach (Canvas excluded in excludedCanvases)
            {
                if (canvas == excluded)
                {
                    isExcluded = true;
                    break;
                }
            }

            if (!isExcluded)
            {
                canvas.gameObject.SetActive(false);
            }
        }
    }
}
