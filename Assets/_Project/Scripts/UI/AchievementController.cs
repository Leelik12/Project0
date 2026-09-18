using UnityEngine;

/// <summary>Экран достижений: включает иконки выполненных достижений по статистике из GameState.</summary>
public class AchievementController : MonoBehaviour
{
    public GameObject s100;
    public GameObject s500;
    public GameObject h100;
    public GameObject h500;
    public GameObject d1000;
    public GameObject d5000;
    public GameObject Cyborg;
    public GameObject FirstDeath;

    private void OnEnable()
    {
        Refresh();
    }

    private void Update()
    {
        Refresh(); // статистика может меняться прямо в меню (тир)
    }

    private void Refresh()
    {
        Show(s100, GameState.countShots >= 100);
        Show(s500, GameState.countShots >= 500);
        Show(h100, GameState.countHits >= 100);
        Show(h500, GameState.countHits >= 500);
        Show(d1000, GameState.Damage >= 1000);
        Show(d5000, GameState.Damage >= 5000);
        Show(Cyborg, GameState.Ciborg);
        Show(FirstDeath, GameState.DiedinCyberpunk);
    }

    private static void Show(GameObject icon, bool achieved)
    {
        if (icon != null && achieved && !icon.activeSelf) icon.SetActive(true);
    }
}
