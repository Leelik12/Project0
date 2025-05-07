using UnityEngine;

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
    public bool podbor = false;

    void Start()
    {
        ActivateRandomCanvas(weaponCanvasesPlace1);
        ActivateRandomCanvas(implantCanvasesPlace2);
        ActivateRandomCanvas(weaponUpgradeCanvasesPlace3);
        ActivateRandomCanvas(allMixedCanvasesPlace4);
        if (podbor)
            ActivateRandomCanvas(allMixedCanvasesPlace5);
    }

    void ActivateRandomCanvas(GameObject[] canvases)
    {
        if (canvases == null || canvases.Length == 0) return;

        foreach (var canvas in canvases)
        {
            if (canvas != null)
                canvas.SetActive(false);
        }

        int randomIndex = Random.Range(0, canvases.Length);
        canvases[randomIndex].SetActive(true);
        Debug.Log("Канвас активирован");
    }
}
