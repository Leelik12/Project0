using System.Collections;
using UnityEngine;

/// <summary>Пулемёты вертолёта: очереди из всех точек стрельбы с разбросом, hitscan-урон по игроку.</summary>
public class HelicopterGunSystem : MonoBehaviour
{
    [Header("Настройки точек стрельбы")]
    public Transform[] firePoints;

    [Header("Настройки стрельбы")]
    public float fireRate = 0.1f;      // интервал между выстрелами в очереди
    public float burstInterval = 4f;   // пауза между очередями
    public int burstCountMin = 15;
    public int burstCountMax = 20;
    public float damage = 10f;
    public float range = 100f;
    public float spreadAngle = 5f;

    [Header("Эффекты")]
    public ParticleSystem[] muzzleFlashes;
    public Light[] muzzleLights;
    public float lightDuration = 0.05f;

    [Header("Декали")]
    public GameObject hitEffectDust;
    public GameObject hitEffectSparks;
    public float hitEffectLifetime = 5f;
    public float effectOffset = 0.01f;

    [Header("Звук")]
    public AudioSource audioSource;
    public AudioClip shotSound;

    [Header("Цель и автоогонь")]
    public bool autoFire = true;
    public string targetTag = "Player";

    private Coroutine burstCoroutine;

    private void Start()
    {
        if (autoFire) burstCoroutine = StartCoroutine(FireBurstsLoop());
    }

    public void ActivateWeapons()
    {
        if (burstCoroutine != null) return;
        autoFire = true;
        burstCoroutine = StartCoroutine(FireBurstsLoop());
    }

    public void DeactivateWeapons()
    {
        autoFire = false;
        if (burstCoroutine != null)
        {
            StopCoroutine(burstCoroutine);
            burstCoroutine = null;
        }
    }

    private IEnumerator FireBurstsLoop()
    {
        while (autoFire)
        {
            int shotsInBurst = Random.Range(burstCountMin, burstCountMax + 1);
            for (int i = 0; i < shotsInBurst; i++)
            {
                for (int p = 0; p < firePoints.Length; p++) Fire(firePoints[p], p);
                yield return new WaitForSeconds(fireRate);
            }
            yield return new WaitForSeconds(burstInterval);
        }
        burstCoroutine = null;
    }

    private void Fire(Transform firePoint, int index)
    {
        if (muzzleFlashes != null && index < muzzleFlashes.Length && muzzleFlashes[index] != null)
            muzzleFlashes[index].Play();
        if (muzzleLights != null && index < muzzleLights.Length && muzzleLights[index] != null)
            StartCoroutine(MuzzleLightFlash(muzzleLights[index]));
        if (audioSource != null && shotSound != null)
            audioSource.PlayOneShot(shotSound);

        Vector3 direction = GetSpreadDirection(firePoint.forward, spreadAngle);
        if (!Physics.Raycast(firePoint.position, direction, out RaycastHit hit, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return;

        Vector3 pos = hit.point + hit.normal * effectOffset;
        Quaternion rot = Quaternion.LookRotation(hit.normal);
        if (hitEffectDust != null) Destroy(Instantiate(hitEffectDust, pos, rot), hitEffectLifetime);
        if (hitEffectSparks != null) Destroy(Instantiate(hitEffectSparks, pos, rot), hitEffectLifetime);

        if (hit.collider.CompareTag(targetTag))
        {
            PlayerHealth player = hit.collider.GetComponentInParent<PlayerHealth>();
            if (player != null) player.PlayerTakeDamage(damage);
        }
    }

    private static Vector3 GetSpreadDirection(Vector3 forward, float angle)
    {
        float spreadX = Random.Range(-angle, angle);
        float spreadY = Random.Range(-angle, angle);
        return Quaternion.Euler(spreadY, spreadX, 0) * forward;
    }

    private IEnumerator MuzzleLightFlash(Light muzzleLight)
    {
        muzzleLight.enabled = true;
        yield return new WaitForSeconds(lightDuration);
        muzzleLight.enabled = false;
    }
}
