using MikeNspired.XRIStarterKit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class VRGun : MonoBehaviour
{
    [SerializeField] private XRGrabInteractable grabInteractable;
    [Header("Перезарядка")]
    [SerializeField] public int maxAmmo;
    public int currentAmmo;
    public Transform magazineSocket; // Точка, куда вставляется магазин
    public bool IsLoaded => currentAmmo > 0;
    private VRMagazine currentMagazine;
    public bool IsCharged = true;
    public GameObject emptyMagazinePrefab; // Префаб пустого магазина
    public Transform ejectPoint; // Точка, откуда выпадает магазин
    [SerializeField] private GameObject internalMagazineModel; // Встроенный визуальный магазин (активируется/скрывается)

    [Header("Подвижные части")]
    [SerializeField] private Transform movablePart; // Подвижная часть оружия (затвор)
    [SerializeField] private float recoilDistance = 0.2f; // Расстояние, на которое подвижная часть будет двигаться
    [SerializeField] private float recoilDuration = 0.1f;     // сколько длится отдача (сек)

    [Header("Настройки стрельбы")]
    public float fireRate = 0.1f;
    public float damage = 10f;
    public float range = 100f;

    [Header("Настройки лазера")]
    public bool laserEnabled = true;
    public LineRenderer laserLine; // Сюда подключается LineRenderer в инспекторе

    [Header("Muzzle Flash")]
    public ParticleSystem muzzleFlash;
    public Light muzzleLight;
    public float lightDuration; // длительность вспышки света

    [Header("XR")]
    public InputActionProperty triggerAction; // <-- сюда привязываем Input Action с триггера
    public Transform firePoint;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip shotSound;

    [Header("Прочее")]
    public string enemyTag = ""; // Тег врага
    private float nextFireTime = 0f;


    void Awake()
    {
        triggerAction.action.Enable();
    }
    void Update()
    {

        if (currentAmmo <= 0 && IsCharged)
        {
            IsCharged = false;
            EjectMagazine(); // автоматически выбрасывает магазин при окончании патронов
        }
        // Проверка ввода через Input System
        if (triggerAction.action != null && triggerAction.action.ReadValue<float>() > 0.8f && Time.time >= nextFireTime && grabInteractable.isSelected && IsLoaded)
        {
            Debug.Log("Выстрел!");
            nextFireTime = Time.time + fireRate;
            Shoot();
        }

        if (laserEnabled && laserLine != null)
        {
            UpdateLaser();
        }
        else if (laserLine != null)
        {
            laserLine.enabled = false;
        }
    }

    void Shoot()
    {
        currentAmmo--;
        if (currentAmmo <= 0)
        {
            Debug.Log("Выстрел! Осталось патронов: " + currentAmmo);
        }
        // Визуальный эффект
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }
        if (muzzleLight != null)
            StartCoroutine(MuzzleLightFlash());
        // Звук
        if (audioSource != null && shotSound != null)
            audioSource.PlayOneShot(shotSound);
        StartCoroutine(MoveRecoil());
        RaycastHit hit;
        if (Physics.Raycast(firePoint.position, firePoint.forward, out hit, range))
        {
            if (hit.collider.CompareTag("Head") || hit.collider.CompareTag("Leg") || hit.collider.CompareTag("Body"))
            {
                float finalDamage = damage;
                // Определяем зону попадания
                string hitPartName = hit.collider.name.ToLower();


                if (hit.collider.CompareTag("Head"))
                {
                    finalDamage *= 2f;
                    Debug.Log("Headshot!");
                }
                else if (hitPartName.Contains("leg"))
                {
                    finalDamage *= 0.5f;
                    Debug.Log("Leg shot!");

                    // Замедляем врага
                    EnemyStateManager movement = hit.collider.GetComponentInParent<EnemyStateManager>();
                    if (movement != null)
                    {
                        movement.walkSpeed = movement.walkSpeed * 0.9f;
                        movement.runSpeed = movement.runSpeed * 0.9f;
                    }
                }
                else
                {
                    // тело — обычный урон
                    Debug.Log("Body shot!");
                }

                EnemyHeaths target = hit.collider.GetComponentInParent<EnemyHeaths>();
                if (target != null)
                {
                    target.TakeDamage(finalDamage);
                }
            }
        }
    }
    public bool CanInsertMagazine()
    {
        return currentMagazine == null;
    }
    public void InsertMagazine(VRMagazine magazine)
    {
        IsCharged = true;
        if (currentMagazine != null) return;

        currentMagazine = magazine;
        currentAmmo = Mathf.Min(magazine.ammoAmount, maxAmmo);

        // Прячем внешний магазин
        magazine.gameObject.SetActive(false);

        // Активируем встроенный визуальный магазин
        if (internalMagazineModel != null)
            internalMagazineModel.SetActive(true);

        Debug.Log("Магазин вставлен. Патроны: " + currentAmmo);
    }
    public void EjectMagazine()
    {
        // Выкинуть пустой магазин
        if (currentMagazine == null && emptyMagazinePrefab != null && ejectPoint != null)
        {
            Instantiate(emptyMagazinePrefab, ejectPoint.position, ejectPoint.rotation);
        }
        Debug.Log("Встроенный магазин ВЫКЛ");
        // Выключить визуальный встроенный магазин
        internalMagazineModel.SetActive(false);
        internalMagazineModel.gameObject.SetActive(false);
        Debug.Log("Отключаем встроенный магазин: " + internalMagazineModel.name);
        currentAmmo = 0;

        Debug.Log("Магазин выброшен.");
    }
    IEnumerator MuzzleLightFlash()
    {
        muzzleLight.enabled = true;
        yield return new WaitForSeconds(lightDuration);
        muzzleLight.enabled = false;
    }
    private IEnumerator MoveRecoil()
    {
        // 1) Сохраняем исходную локальную позицию
        Vector3 originalLocalPos = movablePart.localPosition;

        // 2) Чистый локальный вектор отдачи: назад по локальной Z
        Vector3 recoilOffsetLocal = new Vector3(0f, 0f, -recoilDistance);

        float halfDur = recoilDuration * 0.5f;
        float timer = 0f;

        // 3) Двигаем затвор назад (половина отдачи)
        while (timer < halfDur)
        {
            float t = timer / halfDur; 
            movablePart.localPosition = Vector3.Lerp(originalLocalPos,
                                                     originalLocalPos + recoilOffsetLocal,
                                                     t);
            timer += Time.deltaTime;
            yield return null;
        }

        // 4) Возвращаем затвор в исходное положение (половина возврата)
        timer = 0f;
        while (timer < halfDur)
        {
            float t = timer / halfDur;
            movablePart.localPosition = Vector3.Lerp(originalLocalPos + recoilOffsetLocal,
                                                     originalLocalPos,
                                                     t);
            timer += Time.deltaTime;
            yield return null;
        }

        // 5) Гарантируем точно исходную позицию
        movablePart.localPosition = originalLocalPos;
    }

    void UpdateLaser()
    {
        RaycastHit hit;
        Vector3 endPoint;

        if (Physics.Raycast(firePoint.position, firePoint.forward, out hit, range))
        {
            endPoint = hit.point;
        }
        else
        {
            endPoint = firePoint.position + firePoint.forward * range;
        }

        laserLine.enabled = true;
        laserLine.SetPosition(0, firePoint.position);
        laserLine.SetPosition(1, endPoint);
    }
}
