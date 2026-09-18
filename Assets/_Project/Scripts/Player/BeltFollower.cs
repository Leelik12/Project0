using UnityEngine;

/// <summary>
/// Держит объект (пояс) на фиксированном смещении от головы игрока, поворачивая его только по горизонтали.
/// </summary>
public class BeltFollower : MonoBehaviour
{
    public Transform headTransform; // Main Camera
    public Vector3 localOffset = new Vector3(0, -0.5f, 0.3f);

    private void LateUpdate()
    {
        if (headTransform == null) return;

        Vector3 flatForward = Vector3.ProjectOnPlane(headTransform.forward, Vector3.up);
        if (flatForward.sqrMagnitude < 0.0001f) return; // игрок смотрит строго вверх/вниз

        Quaternion flatRotation = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
        transform.SetPositionAndRotation(headTransform.position + flatRotation * localOffset, flatRotation);
    }
}
