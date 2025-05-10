using UnityEngine;

public class BossTriggerZone : MonoBehaviour
{
    public HelicopterBoss helicopterBoss; // —сылка на вертолЄт

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            helicopterBoss.ActivateBoss();
            Destroy(gameObject);
        }
    }
}