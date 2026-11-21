using UnityEngine;

/// <summary>
/// Коллайдер щита щитовика. Без тегов Head/Body/Leg, поэтому оружие игрока не засчитывает по нему урон,
/// а передаёт попадание сюда (VRGun.ApplyDamage, MeleeWeaponDamage). Сам щит и его прочность — в ShieldEnemy.
/// </summary>
public class ShieldPlate : MonoBehaviour
{
    [SerializeField] private ShieldEnemy owner;

    /// <summary>Щит принял удар: урон по врагу не проходит, но щит накапливает повреждения.</summary>
    public void Absorb(Vector3 point, float damage)
    {
        if (owner != null) owner.Absorb(point, damage);
    }
}
