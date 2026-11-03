using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Экранный эффект активного импланта через постобработку:
/// Сандевистан — зеленоватый тон, хроматическая аберрация, лёгкое искажение линзы и виньетка;
/// оптический камуфляж — холодный обесцвеченный тон и голубая виньетка.
/// Глобальный Volume с профилем создаётся из кода (PlayerImplants), плавно включается и выключается по реальному времени.
/// Пока эффект виден, на камере принудительно включена постобработка, потом прежняя настройка возвращается.
/// </summary>
public class ImplantScreenEffect : MonoBehaviour
{
    public enum Style
    {
        Sandevistan,
        OpticalCamo,
    }

    private const float FadeTime = 0.25f;

    private Volume volume;
    private float target;
    private UniversalAdditionalCameraData cameraData;
    private bool holdsPostProcessing;

    // Сандевистан и камуфляж могут работать одновременно: исходное состояние камеры запоминает первый,
    // возвращает последний
    private static int postProcessingUsers;
    private static bool cameraHadPostProcessing;

    public static ImplantScreenEffect Create(Transform owner, Style style)
    {
        var go = new GameObject(style + "Effect");
        go.transform.SetParent(owner, false);
        var effect = go.AddComponent<ImplantScreenEffect>();
        effect.Build(style);
        return effect;
    }

    private void Build(Style style)
    {
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        if (style == Style.Sandevistan) BuildSandevistan(profile);
        else BuildOpticalCamo(profile);

        volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f;
        volume.sharedProfile = profile;
        volume.weight = 0f;
        volume.enabled = false;
    }

    private static void BuildSandevistan(VolumeProfile profile)
    {
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
    }

    private static void BuildOpticalCamo(VolumeProfile profile)
    {
        ColorAdjustments color = profile.Add<ColorAdjustments>(true);
        color.colorFilter.Override(new Color(0.75f, 0.9f, 1f));
        color.saturation.Override(-60f);

        ChromaticAberration aberration = profile.Add<ChromaticAberration>(true);
        aberration.intensity.Override(0.35f);

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.color.Override(new Color(0.2f, 0.55f, 0.9f));
        vignette.intensity.Override(0.4f);
        vignette.smoothness.Override(0.8f);
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
        if (holdsPostProcessing) return;

        Camera cam = Camera.main;
        if (cam == null || !cam.TryGetComponent(out cameraData)) return;

        holdsPostProcessing = true;
        if (postProcessingUsers++ == 0) cameraHadPostProcessing = cameraData.renderPostProcessing;
        cameraData.renderPostProcessing = true;
    }

    private void RestorePostProcessing()
    {
        // Счётчик уменьшаем, даже если камеру уже уничтожили вместе со сценой, иначе он застрянет
        if (!holdsPostProcessing) return;
        holdsPostProcessing = false;
        if (--postProcessingUsers == 0 && cameraData != null) cameraData.renderPostProcessing = cameraHadPostProcessing;
        cameraData = null;
    }

    // Без перезагрузки домена статика переживает выход из Play Mode
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => postProcessingUsers = 0;
}
