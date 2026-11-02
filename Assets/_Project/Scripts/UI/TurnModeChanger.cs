using UnityEngine;
using UnityEngine.SceneManagement;
using XR.Interaction.Toolkit.Samples;

/// <summary>
/// Переключатель поворота в настройках: плавный (стик крутит непрерывно) или пошаговый (snap, меньше укачивает).
/// Кнопки вызывают SmoothTurn / SnapTurn. Выбор применяется к контроллерам в каждой загружаемой сцене.
/// </summary>
public class TurnModeChanger : MonoBehaviour
{
    public GameObject SmoothTurnCheck; // галочка «Плавный»
    public GameObject SnapTurnCheck;   // галочка «Пошаговый»

    private void Start()
    {
        Apply(GameState.SmoothTurn);
    }

    public void SmoothTurn() => Apply(true);
    public void SnapTurn() => Apply(false);

    private void Apply(bool smooth)
    {
        GameState.SmoothTurn = smooth;
        if (SmoothTurnCheck != null) SmoothTurnCheck.SetActive(smooth);
        if (SnapTurnCheck != null) SnapTurnCheck.SetActive(!smooth);
        ApplyToControllers();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        ApplyToControllers();
        SceneManager.sceneLoaded += (_, _) => ApplyToControllers();
    }

    private static void ApplyToControllers()
    {
        foreach (var manager in FindObjectsByType<ControllerInputActionManager>(FindObjectsSortMode.None))
        {
            manager.smoothTurnEnabled = GameState.SmoothTurn;
        }
    }
}
