using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Отдача в руку: ствол подбрасывает вверх и назад, потом он плавно возвращается.
/// Смещается точка хвата контроллера (attach transform интерактора) — XRI сам тянет оружие за ней,
/// поэтому отдача работает с любым типом движения захвата. Если XRI перезаписал точку хвата,
/// новое положение принимается за исходное, чтобы не бороться с ним.
/// </summary>
public class WeaponRecoil
{
    private const float MaxStack = 3f; // очередь не задирает ствол больше, чем на 3 выстрела

    private readonly float kickAngle;
    private readonly float kickBack;
    private readonly float returnSpeed;

    private float angle;
    private float back;

    private Transform target;
    private Vector3 basePosition;
    private Quaternion baseRotation;
    private Vector3 writtenPosition;
    private Quaternion writtenRotation;

    public WeaponRecoil(float kickAngle, float kickBack, float returnSpeed)
    {
        this.kickAngle = kickAngle;
        this.kickBack = kickBack;
        this.returnSpeed = returnSpeed;
    }

    /// <summary>Толчок от выстрела. scale — множитель силы (например, имплант «Сильные руки»).</summary>
    public void Kick(float scale)
    {
        angle = Mathf.Min(angle + kickAngle * scale, kickAngle * MaxStack);
        back = Mathf.Min(back + kickBack * scale, kickBack * MaxStack);
    }

    /// <summary>Каждый кадр: grab — оружие, если его держат (иначе null), firePoint — задаёт оси «вверх» и «назад».</summary>
    public void Update(IXRSelectInteractable grab, Transform firePoint, TwoHandGrip twoHand = null)
    {
        Transform attach = grab != null && grab.firstInteractorSelecting != null
            ? grab.firstInteractorSelecting.GetAttachTransform(grab)
            : null;

        if (attach != target)
        {
            Reset();
            if (attach == null) return;
            target = attach;
            CaptureBase();
        }
        if (target == null) return;

        // Кто-то (XRI) сдвинул точку хвата — это новое исходное положение
        if (target.localPosition != writtenPosition || target.localRotation != writtenRotation) CaptureBase();

        float k = 1f - Mathf.Exp(-returnSpeed * Time.deltaTime);
        angle = Mathf.Lerp(angle, 0f, k);
        back = Mathf.Lerp(back, 0f, k);

        Transform parent = target.parent;
        Vector3 axis = parent != null ? parent.InverseTransformDirection(firePoint.right) : firePoint.right;
        Vector3 backward = parent != null ? parent.InverseTransformDirection(-firePoint.forward) : -firePoint.forward;

        // Хват двумя руками: доворачиваем исходную ориентацию так, чтобы ствол смотрел вдоль линии между руками
        Quaternion aimedBase = baseRotation;
        if (twoHand != null && twoHand.TryGetAim(out Vector3 worldLine, out Vector3 lineInAttach, out float weight))
        {
            Quaternion parentRotation = parent != null ? parent.rotation : Quaternion.identity;
            Quaternion handWorld = parentRotation * baseRotation;
            Vector3 handLine = handWorld * lineInAttach;
            Quaternion correction = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(handLine, worldLine), weight);
            aimedBase = Quaternion.Inverse(parentRotation) * correction * handWorld;
        }

        // Минус угла вокруг «вправо» — дуло уходит вверх
        target.localRotation = Quaternion.AngleAxis(-angle, axis) * aimedBase;
        target.localPosition = basePosition + backward * back;
        writtenPosition = target.localPosition;
        writtenRotation = target.localRotation;
    }

    /// <summary>Вернуть точку хвата на место (оружие отпустили или выключили).</summary>
    public void Reset()
    {
        if (target != null)
        {
            target.localPosition = basePosition;
            target.localRotation = baseRotation;
        }
        target = null;
        angle = 0f;
        back = 0f;
    }

    private void CaptureBase()
    {
        basePosition = target.localPosition;
        baseRotation = target.localRotation;
        writtenPosition = basePosition;
        writtenRotation = baseRotation;
    }
}
