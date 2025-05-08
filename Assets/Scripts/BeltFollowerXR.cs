using UnityEngine;

public class BeltFollower : MonoBehaviour
{
    public Transform headTransform; // сюда перетаскиваем Main Camera
    void LateUpdate()
    {
        // Получаем поворот головы только по Y
        Vector3 forward = Vector3.ProjectOnPlane(headTransform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }
}

