using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class VRGun : MonoBehaviour
{
    [Header("Настройки стрельбы")]
    public float fireRate = 0.1f;
    public float damage = 10f;
    public float range = 100f;

    [Header("Настройки лазера")]
    public bool laserEnabled = true;
    public LineRenderer laserLine; // Сюда подключается LineRenderer в инспекторе

    [Header("XR Управление")]
    public InputActionProperty triggerAction; // <-- сюда привязываем Input Action с триггера
    public Transform firePoint;

    [Header("Прочее")]
    public string enemyTag = ""; // Тег врага
    private float nextFireTime = 0f;
    void Awake()
    {
        triggerAction.action.Enable();
    }
    void Update()
    {
        // Проверка ввода через Input System
        if (triggerAction.action != null && triggerAction.action.ReadValue<float>() > 0.8f && Time.time >= nextFireTime)
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
