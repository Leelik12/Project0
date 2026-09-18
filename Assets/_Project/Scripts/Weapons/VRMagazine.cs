using UnityEngine;

/// <summary>Полный магазин: при касании оружия без магазина (тег Weapon) вставляется в него.</summary>
public class VRMagazine : MonoBehaviour
{
    public int ammoAmount = 30;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Weapon")) return;

        VRGun gun = other.GetComponent<VRGun>();
        if (gun == null || !gun.CanInsertMagazine()) return;

        gun.InsertMagazine(this);
        Destroy(gameObject);
    }
}
