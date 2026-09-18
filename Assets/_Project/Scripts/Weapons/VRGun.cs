using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Огнестрельное оружие игрока: hitscan-выстрел по зажатию грип+триггер той руки, в которой оружие,
/// магазины, отдача затвора, лазерный прицел, эффекты попадания.
/// </summary>
public class VRGun : MonoBehaviour
{
    /// <summary>Оружие, которое сейчас держит игрок (для HUD). null — ничего не держит.</summary>
    public static VRGun Held { get; private set; }

    private const float PressThreshold = 0.8f;
    private const float HeadshotMultiplier = 2f;
    private const float LegshotMultiplier = 0.5f;
    private const float LegshotSlowFactor = 0.9f;

    [SerializeField] private XRGrabInteractable grabInteractable;

    [Header("Перезарядка")]
    [SerializeField] public float maxAmmo;
    public float currentAmmo;
    public bool IsLoaded => currentAmmo > 0;
    public bool IsCharged = true;                 // магазин вставлен
    public GameObject emptyMagazinePrefab;        // префаб пустого магазина
    public Transform ejectPoint;                  // откуда выпадает магазин
    [SerializeField] private GameObject internalMagazineModel; // встроенный визуальный магазин

    [Header("Подвижные части")]
    [SerializeField] private Transform movablePart;   // затвор
    [SerializeField] private float recoilDistance = 0.2f;
    [SerializeField] private float recoilDuration = 0.1f;

    [Header("Настройки стрельбы")]
    public float fireRate;   // интервал между выстрелами, сек
    public float damage;
    public float range = 100f;

    [Header("Настройки лазера")]
    public bool laserEnabled = true;
    public LineRenderer laserLine;

    [Header("Muzzle Flash")]
    public ParticleSystem muzzleFlash;
    public Light muzzleLight;
    public float lightDuration;

    [Header("XR")]
    public Transform firePoint;
    public InputActionProperty LeftTrigger;
    public InputActionProperty RightTrigger;
    public InputActionProperty LeftGrip;
    public InputActionProperty RightGrip;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip shotSound;

    [Header("Декали")]
    [SerializeField] private GameObject hitEffectPrefabDust;
    [SerializeField] private GameObject hitEffectPrefabSparks;
    [SerializeField] private float hitEffectLifetime = 100f;
    [SerializeField] private float effectOffset = 0.01f;

    private float nextFireTime;
    private bool LaserAllowed => laserEnabled && laserLine != null && (!GameState.Difficulty || GameState.LaserSightUnlocked);

    private void Awake()
    {
        LeftGrip.action.Enable();
        RightGrip.action.Enable();
        LeftTrigger.action.Enable();
        RightTrigger.action.Enable();

        // Применяем купленные апгрейды
        fireRate *= GameState.BuffGunFireRate;
        damage *= GameState.BuffGunDamage;
        currentAmmo *= GameState.BuffGunMaxAmmo;
        maxAmmo *= GameState.BuffGunMaxAmmo;

        if (laserLine != null) laserLine.enabled = LaserAllowed;
    }

    private void Update()
    {
        bool held = grabInteractable != null && grabInteractable.isSelected;
        if (held) Held = this;
        else if (Held == this) Held = null;

        if (currentAmmo <= 0 && IsCharged)
        {
            IsCharged = false;
            EjectMagazine(); // автоматически выбрасываем магазин, когда патроны кончились
        }

        if (held && IsLoaded && Time.time >= nextFireTime && IsFirePressedForHoldingHand())
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }

        if (LaserAllowed) UpdateLaser();
        else if (laserLine != null) laserLine.enabled = false;
    }

    private void OnDisable()
    {
        if (Held == this) Held = null;
    }

    // Стреляет только та рука, в которой лежит оружие (attachTransform содержит "L" или "R")
    private bool IsFirePressedForHoldingHand()
    {
        string attachName = grabInteractable.attachTransform != null ? grabInteractable.attachTransform.name : null;
        if (attachName == null) return false;

        bool leftHand = attachName.Contains("L") && IsPressed(LeftGrip) && IsPressed(LeftTrigger);
        bool rightHand = attachName.Contains("R") && IsPressed(RightGrip) && IsPressed(RightTrigger);
        return leftHand || rightHand;
    }

    private static bool IsPressed(InputActionProperty action) => action.action.ReadValue<float>() > PressThreshold;

    public void Shoot()
    {
        currentAmmo--;
        GameState.countShots++;

        PlayShotEffects();

        if (!Physics.Raycast(firePoint.position, firePoint.forward, out RaycastHit hit, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return;

        SpawnHitEffects(hit);
        ApplyDamage(hit);
    }

    private void PlayShotEffects()
    {
        if (muzzleFlash != null) muzzleFlash.Play();
        if (muzzleLight != null) StartCoroutine(MuzzleLightFlash());
        if (audioSource != null && shotSound != null) audioSource.PlayOneShot(shotSound);
        StartCoroutine(MoveRecoil());
    }

    private void SpawnHitEffects(RaycastHit hit)
    {
        Vector3 pos = hit.point + hit.normal * effectOffset;
        Quaternion rot = Quaternion.LookRotation(hit.normal);

        if (hitEffectPrefabDust != null) Destroy(Instantiate(hitEffectPrefabDust, pos, rot), hitEffectLifetime);
        if (hitEffectPrefabSparks != null) Destroy(Instantiate(hitEffectPrefabSparks, pos, rot), hitEffectLifetime);
    }

    private void ApplyDamage(RaycastHit hit)
    {
        Collider col = hit.collider;
        if (!col.CompareTag("Head") && !col.CompareTag("Leg") && !col.CompareTag("Body")) return;

        EnemyHealth target = col.GetComponentInParent<EnemyHealth>();
        if (target == null) return;

        float finalDamage = damage;
        if (col.CompareTag("Head"))
        {
            finalDamage *= HeadshotMultiplier;
        }
        else if (col.name.ToLower().Contains("leg"))
        {
            finalDamage *= LegshotMultiplier;

            // Попадание в ногу замедляет врага
            EnemyStateManager enemy = col.GetComponentInParent<EnemyStateManager>();
            if (enemy != null)
            {
                enemy.walkSpeed *= LegshotSlowFactor;
                enemy.runSpeed *= LegshotSlowFactor;
            }
        }

        GameState.countHits++;
        GameState.Damage += finalDamage;
        target.TakeDamage(finalDamage);
    }

    // ---------- Магазины ----------

    public bool CanInsertMagazine() => !IsCharged;

    public void InsertMagazine(VRMagazine magazine)
    {
        IsCharged = true;
        currentAmmo = maxAmmo;

        magazine.gameObject.SetActive(false);
        if (internalMagazineModel != null) internalMagazineModel.SetActive(true);
    }

    public void EjectMagazine()
    {
        if (emptyMagazinePrefab != null && ejectPoint != null)
        {
            Instantiate(emptyMagazinePrefab, ejectPoint.position, ejectPoint.rotation);
        }
        if (internalMagazineModel != null) internalMagazineModel.SetActive(false);
        currentAmmo = 0;
    }

    // ---------- Эффекты ----------

    private IEnumerator MuzzleLightFlash()
    {
        muzzleLight.enabled = true;
        yield return new WaitForSeconds(lightDuration);
        muzzleLight.enabled = false;
    }

    private IEnumerator MoveRecoil()
    {
        if (movablePart == null) yield break;

        Vector3 originalLocalPos = movablePart.localPosition;
        Vector3 recoiledLocalPos = originalLocalPos + new Vector3(0f, 0f, -recoilDistance);
        float halfDur = recoilDuration * 0.5f;

        for (float t = 0f; t < halfDur; t += Time.deltaTime)
        {
            movablePart.localPosition = Vector3.Lerp(originalLocalPos, recoiledLocalPos, t / halfDur);
            yield return null;
        }
        for (float t = 0f; t < halfDur; t += Time.deltaTime)
        {
            movablePart.localPosition = Vector3.Lerp(recoiledLocalPos, originalLocalPos, t / halfDur);
            yield return null;
        }
        movablePart.localPosition = originalLocalPos;
    }

    private void UpdateLaser()
    {
        Vector3 endPoint = Physics.Raycast(firePoint.position, firePoint.forward, out RaycastHit hit, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            ? hit.point
            : firePoint.position + firePoint.forward * range;

        laserLine.enabled = true;
        laserLine.SetPosition(0, firePoint.position);
        laserLine.SetPosition(1, endPoint);
    }
}
