using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Спавнит в начале уровня выбранное оружие (GameState.CurrentGun), второй экземпляр при акимбо,
/// магазины к нему и гранату, если куплена.
/// </summary>
public class WeaponSpawnManager : MonoBehaviour
{
    private const float GrenadeSlotSpacing = 0.15f; // м между гранатами на точке спавна

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
    [FormerlySerializedAs("Grenade")] public Transform grenadeSpawnPoint;

    [Header("Префабы оружия")]
    public GameObject AKM;
    public GameObject LaserPistol;
    public GameObject LaserRifle;
    public GameObject Pistol;
    public GameObject P40;
    public GameObject Shotgun;
    public GameObject CopBaton;
    [FormerlySerializedAs("Beat")] public GameObject BaseballBat;
    [FormerlySerializedAs("Grenad")] public GameObject GrenadePrefab;
    public GameObject GrenadeEmpPrefab;
    public GameObject GrenadeSmokePrefab;
    public GameObject GrenadeStickyPrefab;

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

        if (GameState.CurrentGrenade) SpawnAt(GrenadePrefab, grenadeSpawnPoint);

        // Гранаты W-07 лежат рядом с обычной, каждая следующая сдвигается вправо
        int slot = 1;
        if (GameState.GrenadeEmp) SpawnGrenadeNext(GrenadeEmpPrefab, slot++);
        if (GameState.GrenadeSmoke) SpawnGrenadeNext(GrenadeSmokePrefab, slot++);
        if (GameState.GrenadeSticky) SpawnGrenadeNext(GrenadeStickyPrefab, slot);
    }

    private void SpawnGrenadeNext(GameObject prefab, int slot)
    {
        if (prefab == null || grenadeSpawnPoint == null) return;
        Instantiate(prefab, grenadeSpawnPoint.position + grenadeSpawnPoint.right * (GrenadeSlotSpacing * slot), grenadeSpawnPoint.rotation);
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
            case 8: weapon = BaseballBat;                            return true;
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
