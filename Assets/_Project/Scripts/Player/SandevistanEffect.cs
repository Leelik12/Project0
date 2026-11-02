using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Визуал Сандевистана: зеленоватый тон, хроматическая аберрация, лёгкое искажение линзы и виньетка.
/// Глобальный Volume с профилем создаётся из кода (PlayerHealth), плавно включается и выключается по реальному времени.
/// Пока эффект виден, на камере принудительно включена постобработка, потом прежняя настройка возвращается.
/// </summary>
public class SandevistanEffect : MonoBehaviour
{
    private const float FadeTime = 0.25f;

    private Volume volume;
    private float target;
    private UniversalAdditionalCameraData cameraData;
    private bool cameraHadPostProcessing;

    public static SandevistanEffect Create(Transform owner)
    {
        var go = new GameObject("SandevistanEffect");
        go.transform.SetParent(owner, false);
        return go.AddComponent<SandevistanEffect>();
    }

    private void Awake()
    {
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();

        ColorAdjustments color = profile.Add<ColorAdjustments>(true);
        color.colorFilter.Override(new Color(0.75f, 1f, 0.82f));
        color.saturation.Override(-25f);
        color.contrast.Override(15f);

        ChromaticAberration aberration = profile.Add<ChromaticAberration>(true);
        aberration.intensity.Override(0.8f);

        LensDistortion lens = profile.Add<LensDistortion>(true);
        lens.intensity.Override(-0.2f);

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.color.Override(new Color(0.1f, 0.6f, 0.3f));
        vignette.intensity.Override(0.35f);
        vignette.smoothness.Override(0.6f);

        volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f;
        volume.sharedProfile = profile;
        volume.weight = 0f;
        volume.enabled = false;
    }

    /// <summary>Плавно показать или убрать эффект.</summary>
    public void SetActive(bool active)
    {
        target = active ? 1f : 0f;
        if (active)
        {
            volume.enabled = true;
            EnablePostProcessing();
        }
    }

    private void Update()
    {
        if (!volume.enabled) return;

        volume.weight = Mathf.MoveTowards(volume.weight, target, Time.unscaledDeltaTime / FadeTime);
        if (target == 0f && volume.weight <= 0f)
        {
            volume.enabled = false;
            RestorePostProcessing();
        }
    }

    private void OnDestroy()
    {
        RestorePostProcessing();
        if (volume != null && volume.sharedProfile != null) Destroy(volume.sharedProfile);
    }

    private void EnablePostProcessing()
    {
        if (cameraData != null) return;

        Camera cam = Camera.main;
        if (cam == null || !cam.TryGetComponent(out cameraData)) return;

        cameraHadPostProcessing = cameraData.renderPostProcessing;
        cameraData.renderPostProcessing = true;
    }

    private void RestorePostProcessing()
    {
        if (cameraData == null) return;
        cameraData.renderPostProcessing = cameraHadPostProcessing;
        cameraData = null;
    }
}
