using UnityEngine;

public enum ModuleKind
{
    Room,
    Corridor
}

/// <summary>
/// Заготовка комнаты или коридора, из которых собирается этаж. Вешается на корень префаба.
/// Проёмы (<see cref="ModuleConnector"/>) и точки спавна (<see cref="EnemySpawnPoint"/>)
/// берутся из дочерних объектов автоматически.
/// Модуль собирается выровненным по осям: проёмы смотрят строго по ±X или ±Z, масштаб корня 1.
/// </summary>
public class RoomModule : MonoBehaviour
{
    public ModuleKind kind = ModuleKind.Room;

    [Tooltip("Габарит модуля в его локальных координатах — по нему проверяются пересечения. " +
             "Пусто — посчитается по мешам. Посчитать вручную: ⋮ → «Рассчитать габарит».")]
    public Bounds bounds;

    [Tooltip("Только для входной комнаты: где и куда лицом появляется игрок.")]
    public Transform playerStart;

    public ModuleConnector[] Connectors => GetComponentsInChildren<ModuleConnector>(true);
    public EnemySpawnPoint[] SpawnPoints => GetComponentsInChildren<EnemySpawnPoint>(true);

    /// <summary>Габарит для проверки пересечений: заданный вручную или посчитанный по мешам.</summary>
    public Bounds LocalBounds => bounds.size != Vector3.zero ? bounds : CalculateLocalBounds();

    /// <summary>
    /// Габарит всех мешей модуля в его локальных координатах.
    /// Считается по MeshFilter, а не по Renderer.bounds — так работает и на префабе без экземпляра.
    /// </summary>
    public Bounds CalculateLocalBounds()
    {
        Matrix4x4 worldToRoot = transform.worldToLocalMatrix;
        Bounds result = default;
        bool hasAny = false;

        foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;

            Matrix4x4 meshToRoot = worldToRoot * filter.transform.localToWorldMatrix;
            Bounds mesh = filter.sharedMesh.bounds;

            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 local = new Vector3(
                    (corner & 1) == 0 ? mesh.min.x : mesh.max.x,
                    (corner & 2) == 0 ? mesh.min.y : mesh.max.y,
                    (corner & 4) == 0 ? mesh.min.z : mesh.max.z);
                Vector3 point = meshToRoot.MultiplyPoint3x4(local);

                if (hasAny)
                {
                    result.Encapsulate(point);
                }
                else
                {
                    result = new Bounds(point, Vector3.zero);
                    hasAny = true;
                }
            }
        }

        return hasAny ? result : new Bounds(Vector3.zero, Vector3.one);
    }

    [ContextMenu("Рассчитать габарит")]
    private void RecalculateBounds()
    {
        bounds = CalculateLocalBounds();
    }

    private void OnDrawGizmosSelected()
    {
        Bounds local = LocalBounds;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = kind == ModuleKind.Room ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.8f, 0.2f);
        Gizmos.DrawWireCube(local.center, local.size);

        if (playerStart != null)
        {
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerStart.position + Vector3.up, 0.3f);
            Gizmos.DrawLine(playerStart.position + Vector3.up, playerStart.position + Vector3.up + playerStart.forward);
        }
    }
}
