using MikeNspired.XRIStarterKit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class VRgunForEnemy : MonoBehaviour
{
    [Header("Перезарядка")]
    [SerializeField] public float maxAmmo;
    public float currentAmmo;
    public bool IsLoaded => currentAmmo > 0;
    private VRMagazine currentMagazine;
    public bool IsCharged = true;

    [Header("Подвижные части")]
    [SerializeField] private Transform movablePart;
    [SerializeField] private float recoilDistance = 0.2f;
    [SerializeField] private float recoilDuration = 0.1f;

    [Header("Настройки стрельбы")]
    public float fireRate;
    public float damage;
    public float range = 100f;

    [Header("Разброс выстрелов")]
    [SerializeField] private float spreadAngle = 5f; // угол разброса в градусах

    [Header("Настройки лазера")]
    public bool laserEnabled = true;
    public LineRenderer laserLine;

    [Header("Muzzle Flash")]
    public ParticleSystem muzzleFlash;
    public Light muzzleLight;
    public float lightDuration;

    [Header("XR")]
    public Transform firePoint;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip shotSound;

    [Header("Декали")]
    [SerializeField] private GameObject hitEffectPrefabDust;
    [SerializeField] private GameObject hitEffectPrefabSparks;
    [SerializeField] private float hitEffectLifetime = 100f;
    [SerializeField] private float effectOffset = 0.01f;

    [Header("Прочее")]
    public string enemyTag = "";
    private float nextFireTime = 0f;

    private Vector3 lastShotDirection = Vector3.forward;

    string ap = null;
    EnemyStateManager movement = null;

    public void Shoot()
    {
        // Эффекты
        if (muzzleFlash != null) muzzleFlash.Play();
        if (muzzleLight != null) StartCoroutine(MuzzleLightFlash());
        if (audioSource != null && shotSound != null) audioSource.PlayOneShot(shotSound);
        StartCoroutine(MoveRecoil());

        // Направление с разбросом
        Vector3 directionWithSpread = GetSpreadDirection(firePoint.forward, spreadAngle);
        lastShotDirection = directionWithSpread; // сохраняем для лазера

        RaycastHit hit;
        if (Physics.Raycast(firePoint.position, directionWithSpread, out hit, range))
        {
            if (hitEffectPrefabSparks != null)
            {
                Vector3 effectPosition = hit.point + hit.normal * effectOffset;
                Quaternion effectRotation = Quaternion.LookRotation(hit.normal);
                GameObject fx2 = Instantiate(hitEffectPrefabDust, effectPosition, effectRotation);
                GameObject fx3 = Instantiate(hitEffectPrefabSparks, effectPosition, effectRotation);
                Destroy(fx2, hitEffectLifetime);
                Destroy(fx3, hitEffectLifetime);
            }

            if (hit.collider.CompareTag("Player"))
            {
                Debug.Log("В игрока стреляют");
                float finalDamage = damage;
                PlayerHealth player = hit.collider.GetComponentInParent<PlayerHealth>();
                if (player != null)
                {
                    player.PlayerTakeDamage(finalDamage);
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

    IEnumerator MuzzleLightFlash()
    {
        muzzleLight.enabled = true;
        yield return new WaitForSeconds(lightDuration);
        muzzleLight.enabled = false;
    }

    private IEnumerator MoveRecoil()
    {
        if (movablePart == null) yield break;

        Vector3 originalLocalPos = movablePart.localPosition;
        Vector3 recoilOffsetLocal = new Vector3(0f, 0f, -recoilDistance);
        float halfDur = recoilDuration * 0.5f;
        float timer = 0f;

        while (timer < halfDur)
        {
            float t = timer / halfDur;
            movablePart.localPosition = Vector3.Lerp(originalLocalPos, originalLocalPos + recoilOffsetLocal, t);
            timer += Time.deltaTime;
            yield return null;
        }

        timer = 0f;
        while (timer < halfDur)
        {
            float t = timer / halfDur;
            movablePart.localPosition = Vector3.Lerp(originalLocalPos + recoilOffsetLocal, originalLocalPos, t);
            timer += Time.deltaTime;
            yield return null;
        }

        movablePart.localPosition = originalLocalPos;
    }

    public void Update()
    {
        UpdateLaser();
    }

    void UpdateLaser()
    {
        if (!laserEnabled || laserLine == null || firePoint == null)
            return;

        Vector3 direction = lastShotDirection;
        RaycastHit hit;
        Vector3 endPoint;

        if (Physics.Raycast(firePoint.position, direction, out hit, range))
        {
            endPoint = hit.point;
        }
        else
        {
            endPoint = firePoint.position + direction * range;
        }

        laserLine.enabled = true;
        laserLine.SetPosition(0, firePoint.position);
        laserLine.SetPosition(1, endPoint);
    }
}
