using System.Data;
using UnityEngine;

public class Damager : MonoBehaviour
{
    [SerializeField] int damage;
    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<DamageDetector>(out DamageDetector detector))
        {
            detector.OnDamageDetected(damage);
            GetComponent<Collider>().enabled = false;
        }
    }
}
