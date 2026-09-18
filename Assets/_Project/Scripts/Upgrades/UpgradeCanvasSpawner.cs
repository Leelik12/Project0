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

    [Header("Условие для 5 места")]
    public bool podbor = false; // только для отображения в инспекторе

    private void Start()
    {
        ActivateRandomCanvas(weaponCanvasesPlace1);
        ActivateRandomCanvas(implantCanvasesPlace2);
        ActivateRandomCanvas(weaponUpgradeCanvasesPlace3);
        ActivateRandomCanvas(allMixedCanvasesPlace4);

        podbor = GameState.ItemPickedUp;
        if (podbor) ActivateRandomCanvas(allMixedCanvasesPlace5);
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
