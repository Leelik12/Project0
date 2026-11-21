using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>Вид гранаты: осколочная (исходная) и новые из W-07.</summary>
public enum GrenadeKind
{
    Frag,   // осколочная: урон по площади
    Emp,    // ЭМИ: оглушает врагов, сбивает ракеты
    Smoke,  // дымовая: облако, в котором враги не видят игрока
    Sticky, // липкая: прилипает к первой поверхности или врагу, потом взрывается как осколочная
}

/// <summary>
/// Граната: «взводится», если в руке зажать грип+триггер; после этого первое столкновение запускает таймер взрыва.
/// Что происходит при взрыве, задаёт kind (см. GrenadeKind), механика чеки у всех видов одна.
/// </summary>
public class VRGrenade : MonoBehaviour
{
    private const float PressThreshold = 0.8f;
    private static readonly Color EmpColor = new(0.3f, 0.65f, 1f);

    [SerializeField] private GrenadeKind kind = GrenadeKind.Frag;

    [Header("Взрыв")]
    [Tooltip("Секунд от первого столкновения до срабатывания")]
    public float delay = 3f;
    public float explosionRadius = 5f;
    public float explosionForce = 700f;
    public int damage = 100;
    public GameObject explosionEffect;
    public AudioClip explosionSound;

    [Header("ЭМИ")]
    [SerializeField] private float empRadius = 6f;
    [Tooltip("На сколько секунд ЭМИ оглушает врагов")]
    [SerializeField] private float empStunTime = 3f;
    [Tooltip("Эмиссивный материал волны ЭМИ")]
    [SerializeField] private Material empPulseMaterial;

    [Header("Дым")]
    [Tooltip("Префаб облака с SmokeVolume (радиус и время жизни задаются на нём)")]
    [SerializeField] private GameObject smokeCloudPrefab;

    [SerializeField] private XRGrabInteractable grabInteractable;
    public InputActionProperty LeftTrigger;
    public InputActionProperty RightTrigger;
    public InputActionProperty LeftGrip;
    public InputActionProperty RightGrip;

    private bool isArmed;
    private bool timerStarted;
    private bool hasExploded;

    private void Awake()
    {
        LeftGrip.action.Enable();
        RightGrip.action.Enable();
        LeftTrigger.action.Enable();
        RightTrigger.action.Enable();
    }

    private void Update()
    {
        if (isArmed || grabInteractable == null || !grabInteractable.isSelected) return;

        bool left = IsPressed(LeftGrip) && IsPressed(LeftTrigger);
        bool right = IsPressed(RightGrip) && IsPressed(RightTrigger);
        if (left || right)
        {
            Arm();
            Haptics.Pulse(grabInteractable, 0.5f, 0.15f); // чека выдернута
        }
    }

    private static bool IsPressed(InputActionProperty action) => action.action.ReadValue<float>() > PressThreshold;

    /// <summary>Выдернуть чеку (в игре — зажатием грип+триггер).</summary>
    public void Arm() => isArmed = true;

    private void OnCollisionEnter(Collision collision)
    {
        if (!isArmed || timerStarted) return;
        if (kind == GrenadeKind.Sticky && IsHeld) return; // липкая в руке не срабатывает от касания стола или стены

        timerStarted = true;
        if (kind == GrenadeKind.Sticky && !IsHeld) StickTo(collision);
        Invoke(nameof(Explode), delay);
    }

    private bool IsHeld => grabInteractable != null && grabInteractable.isSelected;

    // Липкая: замирает на месте удара и едет вместе с тем, во что попала (врагом, дверью)
    private void StickTo(Collision collision)
    {
        if (TryGetComponent(out Rigidbody body))
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
        }
        foreach (Collider own in GetComponentsInChildren<Collider>()) own.enabled = false; // не мешает и не подбирается

        ContactPoint contact = collision.GetContact(0);
        transform.position = contact.point;
        transform.up = contact.normal;
        transform.SetParent(collision.collider.transform, true);
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        transform.SetParent(null, true); // липкая не должна исчезнуть вместе с тем, к чему прилипла

        Vector3 position = transform.position;
        if (explosionSound != null) AudioSource.PlayClipAtPoint(explosionSound, position);

        switch (kind)
        {
            case GrenadeKind.Emp: EmpBlast(position); break;
            case GrenadeKind.Smoke: SmokeBurst(position); break;
            default: FragBlast(position); break;
        }

        Destroy(gameObject);
    }

    // ---------- ЭМИ ----------

    private void EmpBlast(Vector3 position)
    {
        ExplosionFlash.Spawn(position, empRadius, EmpColor, 8f);
        GrenadePulse.Spawn(position, empRadius, empPulseMaterial);

        var stunned = new HashSet<EnemyStateManager>();
        var shotDown = new HashSet<HelicopterMissile>();
        Collider[] found = Physics.OverlapSphere(position, empRadius); // разовый взрыв: массив можно выделить
        for (int i = 0; i < found.Length; i++)
        {
            Collider nearby = found[i];

            EnemyStateManager enemy = nearby.GetComponentInParent<EnemyStateManager>();
            if (enemy != null && stunned.Add(enemy)) enemy.Stun(empStunTime);

            // Ракеты вертолёта — электроника: ЭМИ выводит их из строя
            HelicopterMissile missile = nearby.GetComponentInParent<HelicopterMissile>();
            if (missile != null && shotDown.Add(missile)) missile.Fizzle();
        }
    }

    // ---------- Дым ----------

    private void SmokeBurst(Vector3 position)
    {
        if (smokeCloudPrefab != null) Instantiate(smokeCloudPrefab, position, Quaternion.identity);
    }

    // ---------- Осколочная и липкая ----------

    private void FragBlast(Vector3 position)
    {
        if (explosionEffect != null)
        {
            GameObject explosion = Instantiate(explosionEffect, position, Quaternion.identity);
            ParticleSystem ps = explosion.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                Destroy(explosion, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(explosion, 5f);
            }
        }

        // У врага несколько коллайдеров (голова, тело, ноги) — урон каждому врагу наносим один раз
        var damaged = new HashSet<EnemyHealth>();
        foreach (Collider nearby in Physics.OverlapSphere(position, explosionRadius))
        {
            if (nearby.CompareTag("Enemy") || nearby.CompareTag("Body"))
            {
                EnemyHealth enemy = nearby.GetComponentInParent<EnemyHealth>();
                if (enemy != null && !enemy.IsDead && damaged.Add(enemy))
                {
                    Vector3 toEnemy = enemy.transform.position + Vector3.up - position;
                    enemy.TakeDamage(damage, position, toEnemy.normalized, false);
                    GameState.RegisterDamage(damage);
                }
            }

            // Тела задетых врагов толкаем ниже целиком, после того как они стали рэгдоллами
            Rigidbody rb = nearby.GetComponent<Rigidbody>();
            if (rb != null && !damaged.Contains(nearby.GetComponentInParent<EnemyHealth>()))
                rb.AddExplosionForce(explosionForce, position, explosionRadius);
        }

        foreach (EnemyHealth enemy in damaged)
        {
            if (!enemy.IsDead) continue;
            foreach (Rigidbody body in enemy.GetComponentsInChildren<Rigidbody>())
            {
                if (!body.isKinematic) body.AddExplosionForce(explosionForce, position, explosionRadius, 0.5f);
            }
        }
    }
}
