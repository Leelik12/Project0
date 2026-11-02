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
        Show(s100, GameState.ShotsFired >= 100);
        Show(s500, GameState.ShotsFired >= 500);
        Show(h100, GameState.ShotsHit >= 100);
        Show(h500, GameState.ShotsHit >= 500);
        Show(d1000, GameState.TotalDamage >= 1000);
        Show(d5000, GameState.TotalDamage >= 5000);
        Show(Cyborg, GameState.CyborgAchieved);
        Show(FirstDeath, GameState.DiedInCyberpunk);
    }

    private static void Show(GameObject icon, bool achieved)
    {
        if (icon != null && achieved && !icon.activeSelf) icon.SetActive(true);
    }
}
