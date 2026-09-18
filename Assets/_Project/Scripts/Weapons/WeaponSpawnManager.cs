using UnityEngine;

/// <summary>
/// Спавнит в начале уровня выбранное оружие (GameState.CurrentGun), второй экземпляр при акимбо,
/// магазины к нему и гранату, если куплена.
/// </summary>
public class WeaponSpawnManager : MonoBehaviour
{
    [Header("Точки спавна")]
    public Transform weaponSpawnPoint;
    public Transform weaponSpawnPointAkimbo;
    public Transform ammoSpawnPoint1;
    public Transform ammoSpawnPoint2;
    public Transform ammoSpawnPoint3;
    public Transform ammoSpawnPoint4;
    public Transform ammoSpawnPoint5;
    public Transform ammoSpawnPoint6;
    public Transform ammoSpawnPoint7;
    public Transform ammoSpawnPoint8;
    public Transform ammoSpawnPoint9;
    public Transform Grenade;

    [Header("Префабы оружия")]
    public GameObject AKM;
    public GameObject LaserPistol;
    public GameObject LaserRifle;
    public GameObject Pistol;
    public GameObject P40;
    public GameObject Shotgun;
    public GameObject CopBaton;
    public GameObject Beat;
    public GameObject Grenad;

    [Header("Префабы боеприпасов")]
    public GameObject pistolAmmoPrefab;
    public GameObject AKMAmmoPrefab;
    public GameObject shotgunAmmoPrefab;
    public GameObject P40AmmoPrefab;

    private void Start()
    {
        SpawnItems();
    }

    private void SpawnItems()
    {
        if (!TryGetWeapon(GameState.CurrentGun, out GameObject weapon, out GameObject ammo))
        {
            Debug.LogWarning($"[WeaponSpawnManager] Неизвестный код оружия: {GameState.CurrentGun}");
            return;
        }

        SpawnAt(weapon, weaponSpawnPoint);
        if (GameState.Akimbo) SpawnAt(weapon, weaponSpawnPointAkimbo);

        if (ammo != null)
        {
            foreach (Transform point in new[] { ammoSpawnPoint1, ammoSpawnPoint2, ammoSpawnPoint3, ammoSpawnPoint4, ammoSpawnPoint5,
                                                ammoSpawnPoint6, ammoSpawnPoint7, ammoSpawnPoint8, ammoSpawnPoint9 })
            {
                SpawnAt(ammo, point);
            }
        }

        if (GameState.CurrentGrenade) SpawnAt(Grenad, Grenade);
    }

    // Коды оружия см. в GameState. 4 (граната) — не основное оружие, спавнится отдельно.
    private bool TryGetWeapon(int code, out GameObject weapon, out GameObject ammo)
    {
        ammo = null;
        switch (code)
        {
            case 0: weapon = AKM;         ammo = AKMAmmoPrefab;     return true;
            case 1: weapon = LaserPistol;                           return true;
            case 2: weapon = LaserRifle;                            return true;
            case 3: weapon = Pistol;      ammo = pistolAmmoPrefab;  return true;
            case 5: weapon = P40;         ammo = P40AmmoPrefab;     return true;
            case 6: weapon = Shotgun;     ammo = shotgunAmmoPrefab; return true;
            case 7: weapon = CopBaton;                              return true;
            case 8: weapon = Beat;                                  return true;
            default: weapon = null;                                 return false;
        }
    }

    private static void SpawnAt(GameObject prefab, Transform point)
    {
        if (prefab != null && point != null)
        {
            Instantiate(prefab, point.position, point.rotation);
        }
    }
}
