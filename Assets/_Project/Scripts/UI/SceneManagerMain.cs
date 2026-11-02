using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

/// <summary>
/// Главное меню: переключение панелей, старт и продолжение забега, выход, экраны победы/поражения с итогами забега.
/// Имя класса и методы привязаны к кнопкам в сцене LobbyScene — не переименовывать.
/// </summary>
public class SceneManagerMain : MonoBehaviour
{
    [SerializeField] public GameObject Main;
    [SerializeField] public GameObject Setting;
    [FormerlySerializedAs("Achievm")]
    [SerializeField] public GameObject Achievements;
    [SerializeField] public GameObject End;
    [Tooltip("Экран поражения. Если не назначен, при смерти показывается End.")]
    [SerializeField] public GameObject Defeat;

    [Header("Продолжение забега")]
    [Tooltip("Кнопка «Продолжить» (метод Continue). Видна, только если есть сохранённый забег.")]
    [SerializeField] private GameObject continueButton;

    [Header("Итоги забега")]
    [Tooltip("Тексты на экранах победы/поражения, куда выводится статистика последнего забега.")]
    [SerializeField] private TMP_Text[] runSummaryTexts;

    private void Awake()
    {
        GameState.LoadData(); // до Start других скриптов меню, чтобы они видели сохранённые настройки
    }

    private void Start()
    {
        if (continueButton != null) continueButton.SetActive(GameState.RunInProgress);

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

    /// <summary>Новый забег с первого этажа: прокачка предыдущего (в том числе сохранённого) сбрасывается.</summary>
    public void Begin()
    {
        GameState.ResetRun();
        GameState.CurrentLevel = GameState.FirstFloorScene;
        GameState.SaveData();
        SceneManager.LoadSceneAsync(GameState.FirstFloorScene);
    }

    /// <summary>Продолжить сохранённый забег: магазин или начало этажа, на котором вышли.</summary>
    public void Continue()
    {
        if (!GameState.RunInProgress)
        {
            Begin();
            return;
        }

        int scene = GameState.ContinueScene;
        if (scene != GameState.UpgradeShopScene)
        {
            // Этаж проходится заново с начала
            GameState.ItemPickedUp = false;
            GameState.UpdateLevelEnd = false;
        }
        GameState.ResetPerLevelFlags();
        GameState.RunInProgress = false;
        GameState.SaveData();
        SceneManager.LoadSceneAsync(scene);
    }

    public void Setti() => ShowPanel(Setting);
    public void Achie() => ShowPanel(Achievements);
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

        if (runSummaryTexts == null) return;
        string summary = UiText.Widen(GameState.LastRun.ToString());
        foreach (TMP_Text text in runSummaryTexts)
        {
            if (text != null) text.text = summary;
        }
    }

    private void ShowPanel(GameObject panel)
    {
        foreach (GameObject p in new[] { Main, Setting, Achievements, End, Defeat })
        {
            if (p != null) p.SetActive(p == panel);
        }
    }
}
