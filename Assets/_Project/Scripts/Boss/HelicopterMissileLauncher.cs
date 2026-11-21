using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ракетные пилоны вертолёта: в фазе 2 каждые несколько секунд выпускает залп самонаводящихся ракет.
/// Модель ракеты — префаб missilePrefab (сейчас примитив-заглушка); его можно заменить настоящей моделью.
/// </summary>
public class HelicopterMissileLauncher : MonoBehaviour
{
    [SerializeField] private HelicopterMissile missilePrefab;
    [SerializeField] private Transform[] launchPoints;          // пилоны: ракета вылетает по их оси +Z
    [SerializeField] private Transform target;                  // игрок (камера)
    [SerializeField] private Transform ownerBody;               // тело вертолёта — об него ракета не взрывается

    [Header("Залп")]
    [SerializeField] private float salvoInterval = 8f;          // пауза между залпами
    [SerializeField] private float firstSalvoDelay = 2f;        // задержка до первого залпа после начала фазы 2
    [SerializeField] private int missilesMin = 2;
    [SerializeField] private int missilesMax = 3;
    [SerializeField] private float shotDelay = 0.45f;           // интервал между ракетами в залпе

    private readonly List<HelicopterMissile> active = new();
    private Coroutine routine;
    private int nextPoint;

    /// <summary>Начать залпы (вызывается при входе в фазу 2).</summary>
    public void StartSalvos()
    {
        if (routine == null) routine = StartCoroutine(SalvoLoop());
    }

    /// <summary>Прекратить залпы и взорвать ракеты в воздухе без урона (смерть босса).</summary>
    public void Stop()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        for (int i = active.Count - 1; i >= 0; i--)
        {
            if (active[i] != null) active[i].Fizzle();
        }
        active.Clear();
    }

    private IEnumerator SalvoLoop()
    {
        yield return new WaitForSeconds(firstSalvoDelay);
        while (true)
        {
            int count = Random.Range(missilesMin, missilesMax + 1);
            for (int i = 0; i < count; i++)
            {
                Launch();
                yield return new WaitForSeconds(shotDelay);
            }
            yield return new WaitForSeconds(salvoInterval);
        }
    }

    private void Launch()
    {
        if (missilePrefab == null || target == null || launchPoints == null || launchPoints.Length == 0) return;

        Transform point = launchPoints[nextPoint++ % launchPoints.Length];
        HelicopterMissile missile = Instantiate(missilePrefab, point.position, point.rotation);
        missile.Launch(target, ownerBody);
        active.RemoveAll(m => m == null);
        active.Add(missile);
    }
}
