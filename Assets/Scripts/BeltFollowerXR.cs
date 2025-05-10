using UnityEngine;

public class BeltFollower : MonoBehaviour
{
    public Transform headTransform; // Main Camera
    public Vector3 localOffset = new Vector3(0, -0.5f, 0.3f); // Смещение "вперёд" от головы, как если бы объект был дочерним

    void LateUpdate()
    {
        // Получаем плоское направление взгляда (вектор вперед по Y=0)
        Vector3 flatForward = Vector3.ProjectOnPlane(headTransform.forward, Vector3.up).normalized;
        Vector3 flatRight = Vector3.Cross(Vector3.up, flatForward).normalized;

        // Собираем матрицу горизонтального вращения
        Quaternion flatRotation = Quaternion.LookRotation(flatForward, Vector3.up);

        // Позиция — как если бы offset применялся в этой плоскости
        Vector3 worldOffset = flatRotation * localOffset;
        transform.position = headTransform.position + worldOffset;

        // Вращение — только по Y
        transform.rotation = flatRotation;
    }
}
