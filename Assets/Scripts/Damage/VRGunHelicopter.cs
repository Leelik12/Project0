using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelicopterGunSystem : MonoBehaviour
{
    [Header("Настройки точек стрельбы")]
    public Transform[] firePoints;

    [Header("Настройки стрельбы")]
    public float fireRate = 0.1f;
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

    private float nextFireTime = 0f;

    void Update()
    {
        if (autoFire && Time.time >= nextFireTime)
        {
            FireFromAllPoints();
            nextFireTime = Time.time + fireRate;
        }
    }

    public void FireFromAllPoints()
    {
        for (int i = 0; i < firePoints.Length; i++)
        {
            Fire(firePoints[i], i);
        }
    }

    void Fire(Transform firePoint, int index)
    {
        // Эффекты
        if (muzzleFlashes != null && index < muzzleFlashes.Length && muzzleFlashes[index] != null)
            muzzleFlashes[index].Play();

        if (muzzleLights != null && index < muzzleLights.Length && muzzleLights[index] != null)
            StartCoroutine(MuzzleLightFlash(muzzleLights[index]));

        if (audioSource != null && shotSound != null)
            audioSource.PlayOneShot(shotSound);

        Vector3 direction = GetSpreadDirection(firePoint.forward, spreadAngle);

        if (Physics.Raycast(firePoint.position, direction, out RaycastHit hit, range))
        {
            Vector3 effectPosition = hit.point + hit.normal * effectOffset;
            Quaternion effectRotation = Quaternion.LookRotation(hit.normal);

            if (hitEffectDust != null)
                Destroy(Instantiate(hitEffectDust, effectPosition, effectRotation), hitEffectLifetime);
            if (hitEffectSparks != null)
                Destroy(Instantiate(hitEffectSparks, effectPosition, effectRotation), hitEffectLifetime);

            if (hit.collider.CompareTag(targetTag))
            {
                PlayerHealth player = hit.collider.GetComponentInParent<PlayerHealth>();
                if (player != null)
                {
                    player.PlayerTakeDamage(damage);
                }
            }
        }
    }

    private Vector3 GetSpreadDirection(Vector3 forward, float angle)
    {
        float spreadX = Random.Range(-angle, angle);
        float spreadY = Random.Range(-angle, angle);
        Quaternion rotation = Quaternion.Euler(spreadY, spreadX, 0);
        return rotation * forward;
    }

    IEnumerator MuzzleLightFlash(Light muzzleLight)
    {
        muzzleLight.enabled = true;
        yield return new WaitForSeconds(lightDuration);
        muzzleLight.enabled = false;
    }
}
