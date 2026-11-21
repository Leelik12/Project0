using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// W-07: облако дыма от дымовой гранаты. Сфера растёт за growTime, живёт duration секунд, в конце дым редеет.
/// Пока облако живо, EnemyStateManager.CanSeePlayer не видит игрока, если луч от глаз врага к голове
/// проходит через сферу дыма (в том числе когда игрок или враг стоят внутри неё).
/// Визуал — ParticleSystem на этом же объекте (необязателен).
/// </summary>
public class SmokeVolume : MonoBehaviour
{
    private static readonly List<SmokeVolume> Active = new();

    [Header("Облако")]
    [Tooltip("Радиус непрозрачной для врагов сферы, м")]
    [SerializeField] private float radius = 5f;
    [SerializeField] private float growTime = 1.5f;
    [Tooltip("Сколько секунд облако живёт, считая редеющий хвост")]
    [SerializeField] private float duration = 10f;
    [Tooltip("За сколько секунд до конца дым перестаёт выпускаться")]
    [SerializeField] private float fadeTime = 3f;
    [SerializeField] private ParticleSystem particles;

    private float age;
    private bool emissionStopped;

    /// <summary>Текущий радиус: облако раздувается плавно.</summary>
    public float CurrentRadius => radius
        * Mathf.SmoothStep(0f, 1f, growTime > 0f ? age / growTime : 1f)
        * (fadeTime > 0f ? Mathf.Clamp01((duration - age) / fadeTime) : 1f); // редеющий дым перестаёт закрывать обзор

    private void OnEnable() => Active.Add(this);

    private void OnDisable() => Active.Remove(this);

    private void Update()
    {
        age += Time.deltaTime;

        if (!emissionStopped && age >= duration - fadeTime)
        {
            emissionStopped = true;
            if (particles != null) particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        if (age >= duration) Destroy(gameObject);
    }

    /// <summary>Закрывает ли хоть одно облако прямую линию from→to.</summary>
    public static bool Blocks(Vector3 from, Vector3 to)
    {
        for (int i = 0; i < Active.Count; i++)
        {
            SmokeVolume smoke = Active[i];
            float r = smoke.CurrentRadius;
            if (r <= 0.01f) continue;
            if (SegmentDistanceSqr(smoke.transform.position, from, to) < r * r) return true;
        }
        return false;
    }

    private static float SegmentDistanceSqr(Vector3 point, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float lengthSqr = ab.sqrMagnitude;
        float t = lengthSqr > 1e-6f ? Mathf.Clamp01(Vector3.Dot(point - a, ab) / lengthSqr) : 0f;
        return (a + ab * t - point).sqrMagnitude;
    }
}
