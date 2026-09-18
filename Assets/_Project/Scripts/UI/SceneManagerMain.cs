using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Главное меню: переключение панелей, старт игры, выход, экраны победы/поражения.
/// Имя класса и методы привязаны к кнопкам в сцене LobbyScene — не переименовывать.
/// </summary>
public class SceneManagerMain : MonoBehaviour
{
    private const string FirstLevelScene = "TheFirstLevel";

    [SerializeField] public GameObject Main;
    [SerializeField] public GameObject Setting;
    [SerializeField] public GameObject Achievm;
    [SerializeField] public GameObject End;
    [Tooltip("Экран поражения. Если не назначен, при смерти показывается End.")]
    [SerializeField] public GameObject Defeat;

    private void Awake()
    {
        GameState.LoadData(); // до Start других скриптов меню, чтобы они видели сохранённые настройки
    }

    private void Start()
    {
        if (GameState.GameOver || GameState.PlayerLost)
        {
            ShowEndScreen(lost: GameState.PlayerLost);
            GameState.GameOver = false;
            GameState.PlayerLost = false;
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) GameState.SaveData();
    }

    private void OnApplicationQuit()
    {
        GameState.SaveData();
    }

    public void Begin()
    {
        GameState.SaveData();
        SceneManager.LoadSceneAsync(FirstLevelScene);
    }

    public void Setti() => ShowPanel(Setting);
    public void Achie() => ShowPanel(Achievm);
    public void Bach2Main() => ShowPanel(Main);

    public void Exi()
    {
        GameState.SaveData();
        Application.Quit();
    }

    public void EndClose() => ShowPanel(Main);

    private void ShowEndScreen(bool lost)
    {
        ShowPanel(lost && Defeat != null ? Defeat : End);
    }

    private void ShowPanel(GameObject panel)
    {
        foreach (GameObject p in new[] { Main, Setting, Achievm, End, Defeat })
        {
            if (p != null) p.SetActive(p == panel);
        }
    }
}
