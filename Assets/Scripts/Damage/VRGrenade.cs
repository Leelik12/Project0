using MikeNspired.XRIStarterKit;
using UnityEngine;

public class VRGrenade : MonoBehaviour
{
    [Header("Взрыв")]
    public float delay = 3f;
    public float explosionRadius = 5f;
    public float explosionForce = 700f;
    public int damage = 100;
    public GameObject explosionEffect;
    public AudioClip explosionSound; // звук взрыва

    private bool hasExploded = false;
    private bool timerStarted = false;

    void OnCollisionEnter(Collision collision)
    {
        if (!timerStarted)
        {
            timerStarted = true;
            Invoke(nameof(Explode), delay);
        }
    }

    void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        //  Воспроизведение звука
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }

        //  Визуальный эффект
        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, transform.rotation);

        //  Урон врагам
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider nearbyObject in colliders)
        {
            if (nearbyObject.CompareTag("Enemy")|| nearbyObject.CompareTag("Body"))
            {
                EnemyHeaths enemy = nearbyObject.GetComponentInParent<EnemyHeaths>();
                if (enemy != null)
                    Debug.Log("Урон прошел!");
                    enemy.TakeDamage(damage);
            }

            Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
        }

        Destroy(gameObject);
    }
}
