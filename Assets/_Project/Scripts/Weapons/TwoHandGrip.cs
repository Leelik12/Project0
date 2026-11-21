using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// W-02: хват двумя руками. Винтовка и дробовик можно взять второй рукой за цевьё (точка foregrip):
/// зажать грип рядом с ней. Пока вторая рука держит, ствол смотрит вдоль линии «основная рука — вторая рука»
/// (прицел стабильнее, лёгкая сглаженная фильтрация дрожи), а отдача слабее.
/// Стреляет и перезаряжает по-прежнему только основная рука (VRGun); вторая рука оружие не «крадёт»
/// (см. BlocksSelection). Тик вызывает VRGun.Update, чтобы порядок был строго до WeaponRecoil.
/// </summary>
public class TwoHandGrip : MonoBehaviour
{
    private const float EngageGrip = 0.8f;
    private const float ReleaseGrip = 0.5f;
    private const float MinHandsDistance = 0.08f; // ближе — линия между руками ненадёжна

    [SerializeField] private XRGrabInteractable grabInteractable;
    [SerializeField] private VRGun gun;
    [Tooltip("Точка на цевье, за которую берётся вторая рука")]
    [SerializeField] private Transform foregrip;

    [Header("Хват")]
    [Tooltip("На каком расстоянии от цевья вторая рука «цепляется», м")]
    [SerializeField] private float grabRadius = 0.2f;
    [Tooltip("Дальше этого расстояния хват двумя руками сам отпускается, м")]
    [SerializeField] private float releaseRadius = 0.4f;

    [Header("Эффект")]
    [Tooltip("Множитель отдачи при хвате двумя руками")]
    [SerializeField] private float recoilMultiplier = 0.5f;
    [Tooltip("Сглаживание прицела (больше — резче следует за руками)")]
    [SerializeField] private float aimSmoothing = 18f;
    [Tooltip("За сколько секунд прицел плавно переходит между одно- и двуручным, сек")]
    [SerializeField] private float blendTime = 0.15f;

    private IXRSelectInteractor mainInteractor;
    private NearFarInteractor offHand;
    private InputActionProperty offGrip;
    private Vector3 lineInAttach;   // направление «хват — цевьё» в системе точки хвата
    private Vector3 smoothedLine;
    private Vector3 worldLine;
    private float weight;

    /// <summary>Вторая рука сейчас держит цевьё.</summary>
    public bool IsActive { get; private set; }

    public float RecoilMultiplier => IsActive ? recoilMultiplier : 1f;

    private void Awake()
    {
        if (grabInteractable == null) grabInteractable = GetComponent<XRGrabInteractable>();
        if (gun == null) gun = GetComponent<VRGun>();
    }

    private void OnDisable()
    {
        Release();
        weight = 0f;
    }

    /// <summary>Каждый кадр из VRGun.Update, до WeaponRecoil.Update.</summary>
    public void Tick()
    {
        if (grabInteractable == null || foregrip == null) return;

        if (!grabInteractable.isSelected)
        {
            Release();
            mainInteractor = null;
            offHand = null;
            weight = 0f;
            return;
        }

        IXRSelectInteractor main = grabInteractable.firstInteractorSelecting;
        if (main != mainInteractor)
        {
            Release();
            mainInteractor = main;
            offHand = FindOffHand(main);
            weight = 0f;
        }

        Transform gunAttach = grabInteractable.attachTransform;
        if (offHand != null && gunAttach != null)
        {
            Transform mainAttach = main.GetAttachTransform(grabInteractable);
            Transform offAttach = offHand.GetAttachTransform(grabInteractable);
            if (mainAttach == null || offAttach == null) { Release(); return; }
            float distance = Vector3.Distance(offAttach.position, foregrip.position);
            float grip = ReadOffGrip();

            if (!IsActive)
            {
                if (grip > EngageGrip && distance < grabRadius && !offHand.hasSelection) Engage(gunAttach);
            }
            else if (grip < ReleaseGrip || distance > releaseRadius || offHand.hasSelection)
            {
                Release();
            }

            if (IsActive)
            {
                Vector3 line = offAttach.position - mainAttach.position;
                if (line.sqrMagnitude > MinHandsDistance * MinHandsDistance)
                {
                    float k = 1f - Mathf.Exp(-aimSmoothing * Time.unscaledDeltaTime); // руки игрока не замедляются Сандевистаном и паузой
                    smoothedLine = Vector3.Slerp(smoothedLine, line.normalized, k);
                }
            }
        }

        float step = blendTime > 0f ? Time.unscaledDeltaTime / blendTime : 1f;
        weight = Mathf.MoveTowards(weight, IsActive ? 1f : 0f, step);
    }

    /// <summary>Нужен ли доворот прицела. line — куда должна смотреть линия «хват — цевьё», lineInAttach — она же в системе точки хвата.</summary>
    public bool TryGetAim(out Vector3 line, out Vector3 localLine, out float blend)
    {
        line = smoothedLine;
        localLine = lineInAttach;
        blend = weight;
        return weight > 0.001f;
    }

    /// <summary>Не даём второй руке перехватить оружие, если она тянется к цевью: это хват, а не смена руки.</summary>
    public bool BlocksSelection(IXRSelectInteractor interactor)
    {
        if (foregrip == null || grabInteractable == null || !grabInteractable.isSelected || interactor.IsSelecting(grabInteractable)) return false;
        Transform attach = interactor.GetAttachTransform(grabInteractable);
        return attach != null && Vector3.Distance(attach.position, foregrip.position) < grabRadius;
    }

    private void Engage(Transform gunAttach)
    {
        IsActive = true;
        lineInAttach = (Quaternion.Inverse(gunAttach.rotation) * (foregrip.position - gunAttach.position)).normalized;
        smoothedLine = (gunAttach.rotation * lineInAttach).normalized; // старт с текущей ориентации — без рывка
        Haptics.Pulse(grabInteractable, 0.25f, 0.05f);
    }

    private void Release() => IsActive = false;

    private float ReadOffGrip()
    {
        if (gun == null) return 0f;
        InputActionProperty action = offHand.CompareTag("LeftHand") ? gun.LeftGrip : gun.RightGrip;
        return action.action != null ? action.action.ReadValue<float>() : 0f;
    }

    // Вторая рука — интерактор с противоположным тегом (ищем один раз при хвате, не каждый кадр)
    private static NearFarInteractor FindOffHand(IXRSelectInteractor main)
    {
        if (main is not Component mainComponent) return null;
        string offTag = mainComponent.CompareTag("LeftHand") ? "RightHand" : mainComponent.CompareTag("RightHand") ? "LeftHand" : null;
        if (offTag == null) return null;

        foreach (GameObject candidate in GameObject.FindGameObjectsWithTag(offTag))
        {
            if (candidate.TryGetComponent(out NearFarInteractor interactor)) return interactor;
        }
        return null;
    }
}
