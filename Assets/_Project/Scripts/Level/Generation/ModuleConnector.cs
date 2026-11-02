using UnityEngine;

/// <summary>
/// Проём модуля, через который к нему стыкуется соседний модуль.
/// Вешается на пустой дочерний объект в центре проёма на уровне пола; синяя ось (+Z) смотрит наружу.
/// Проём должен лежать на грани габарита модуля, иначе соседи будут считаться пересекающимися.
/// </summary>
public class ModuleConnector : MonoBehaviour
{
    [Tooltip("Стыкуются только проёмы одинакового размера. Если все проёмы одинаковые — оставь 1.")]
    public int size = 1;

    private void OnDrawGizmos()
    {
        Vector3 position = transform.position;
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(position, position + forward);
        Gizmos.DrawLine(position + forward, position + forward * 0.7f + right * 0.2f);
        Gizmos.DrawLine(position + forward, position + forward * 0.7f - right * 0.2f);

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(new Vector3(0f, 1f, 0f), new Vector3(2f, 2f, 0.05f));
    }
}
