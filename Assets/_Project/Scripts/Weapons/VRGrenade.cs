using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Граната: «взводится», если в руке зажать грип+триггер; после этого первое столкновение запускает таймер взрыва.
/// </summary>
public class VRGrenade : MonoBehaviour
{
    private const float PressThreshold = 0.8f;

    [Header("Взрыв")]
    public float delay = 3f;
    public float explosionRadius = 5f;
    public float explosionForce = 700f;
    public int damage = 100;
    public GameObject explosionEffect;
    public AudioClip explosionSound;

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
        if (left || right) isArmed = true;
    }

    private static bool IsPressed(InputActionProperty action) => action.action.ReadValue<float>() > PressThreshold;

    private void OnCollisionEnter(Collision collision)
    {
        if (isArmed && !timerStarted)
        {
            timerStarted = true;
            Invoke(nameof(Explode), delay);
        }
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }

        if (explosionEffect != null)
        {
            GameObject explosion = Instantiate(explosionEffect, transform.position, Quaternion.identity);
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

        foreach (Collider nearby in Physics.OverlapSphere(transform.position, explosionRadius))
        {
            if (nearby.CompareTag("Enemy") || nearby.CompareTag("Body"))
            {
                EnemyHealth enemy = nearby.GetComponentInParent<EnemyHealth>();
                if (enemy != null) enemy.TakeDamage(damage);
            }

            Rigidbody rb = nearby.GetComponent<Rigidbody>();
            if (rb != null) rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
        }

        Destroy(gameObject);
    }
}
