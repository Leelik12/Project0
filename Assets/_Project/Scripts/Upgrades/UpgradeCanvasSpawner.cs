using UnityEngine;

/// <summary>
/// В магазине апгрейдов включает по одной случайной карточке в каждом из 4 мест.
/// Пятое место открывается, только если на прошлом этаже был найден предмет.
/// </summary>
public class UpgradeCanvasSpawner : MonoBehaviour
{
    [Header("Группы канвасов по местам")]
    public GameObject[] weaponCanvasesPlace1;
    public GameObject[] implantCanvasesPlace2;
    public GameObject[] weaponUpgradeCanvasesPlace3;

    [Tooltip("Здесь комбинируются все типы")]
    public GameObject[] allMixedCanvasesPlace4;
    public GameObject[] allMixedCanvasesPlace5;

    private void Start()
    {
        ActivateRandomCanvas(weaponCanvasesPlace1);
        ActivateRandomCanvas(implantCanvasesPlace2);
        ActivateRandomCanvas(weaponUpgradeCanvasesPlace3);
        ActivateRandomCanvas(allMixedCanvasesPlace4);

        // Пятое место — награда за найденный на этаже предмет
        if (GameState.ItemPickedUp) ActivateRandomCanvas(allMixedCanvasesPlace5);
    }

    private static void ActivateRandomCanvas(GameObject[] canvases)
    {
        if (canvases == null || canvases.Length == 0) return;

        foreach (GameObject canvas in canvases)
        {
            if (canvas != null) canvas.SetActive(false);
        }

        GameObject chosen = canvases[Random.Range(0, canvases.Length)];
        if (chosen != null) chosen.SetActive(true);
    }
}
