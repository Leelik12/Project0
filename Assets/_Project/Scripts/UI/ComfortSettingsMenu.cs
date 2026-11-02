using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;
using XR.Interaction.Toolkit.Samples;

/// <summary>
/// Настройки комфорта в меню:
/// поворот — плавный (стик крутит непрерывно) или пошаговый (snap, меньше укачивает), кнопки SmoothTurn / SnapTurn;
/// виньетка — сужение обзора при движении и поворотах, кнопки VignetteOn / VignetteOff.
/// Выбор сохраняется в GameState и применяется к ригу игрока в каждой загружаемой сцене.
/// </summary>
public class ComfortSettingsMenu : MonoBehaviour
{
    public GameObject SmoothTurnCheck; // галочка «Плавный»
    public GameObject SnapTurnCheck;   // галочка «Пошаговый»
    public GameObject VignetteOnCheck;
    public GameObject VignetteOffCheck;

    private static TunnelingVignetteController vignette;

    private void Start()
    {
        UpdateChecks();
    }

    public void SmoothTurn() => SetTurn(true);
    public void SnapTurn() => SetTurn(false);
    public void VignetteOn() => SetVignette(true);
    public void VignetteOff() => SetVignette(false);

    private void SetTurn(bool smooth)
    {
        GameState.SmoothTurn = smooth;
        UpdateChecks();
        ApplyToRig();
    }

    private void SetVignette(bool on)
    {
        GameState.ComfortVignette = on;
        UpdateChecks();
        ApplyToRig();
    }

    private void UpdateChecks()
    {
        if (SmoothTurnCheck != null) SmoothTurnCheck.SetActive(GameState.SmoothTurn);
        if (SnapTurnCheck != null) SnapTurnCheck.SetActive(!GameState.SmoothTurn);
        if (VignetteOnCheck != null) VignetteOnCheck.SetActive(GameState.ComfortVignette);
        if (VignetteOffCheck != null) VignetteOffCheck.SetActive(!GameState.ComfortVignette);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        ApplyToRig();
        SceneManager.sceneLoaded += (_, _) => ApplyToRig();
    }

    private static void ApplyToRig()
    {
        foreach (var manager in FindObjectsByType<ControllerInputActionManager>(FindObjectsSortMode.None))
        {
            manager.smoothTurnEnabled = GameState.SmoothTurn;
        }

        ApplyVignette();
    }

    // Виньетку берём из XRI Starter Assets (префаб в GameFeedbackSettings) и вешаем на камеру игрока
    private static void ApplyVignette()
    {
        if (vignette == null)
        {
            if (!GameState.ComfortVignette) return;

            GameObject prefab = GameFeedbackSettings.Instance.tunnelingVignettePrefab;
            Camera cam = Camera.main;
            if (prefab == null || cam == null) return;

            GameObject instance = Instantiate(prefab, cam.transform, false);
            vignette = instance.GetComponent<TunnelingVignetteController>();
            if (vignette == null) return;

            var providers = new List<LocomotionVignetteProvider>();
            foreach (LocomotionProvider provider in FindObjectsByType<LocomotionProvider>(FindObjectsSortMode.None))
            {
                providers.Add(new LocomotionVignetteProvider { locomotionProvider = provider, enabled = true });
            }
            vignette.locomotionVignetteProviders = providers;
        }

        vignette.gameObject.SetActive(GameState.ComfortVignette);
    }
}
