using UnityEngine;

/// <summary>Плавно переносит оружие врага между точками хвата в зависимости от состояния (idle/walk/run/attack).</summary>
public class EnemyWeaponController : MonoBehaviour
{
    public Transform weapon;
    public Transform holdIdlePoint;
    public Transform holdRunPoint;
    public Transform holdWalkPoint;
    public Transform holdAttackPoint;

    private const float SmoothTime = 0.2f;
    private const float RotationSpeed = 10f;

    private Transform targetPoint;
    private Vector3 velocity;

    private void Update()
    {
        if (targetPoint == null || weapon == null) return;

        weapon.position = Vector3.SmoothDamp(weapon.position, targetPoint.position, ref velocity, SmoothTime);
        weapon.rotation = Quaternion.Lerp(weapon.rotation, targetPoint.rotation, Time.deltaTime * RotationSpeed);
    }

    public void SetHoldIdle() => targetPoint = holdIdlePoint;
    public void SetHoldRun() => targetPoint = holdRunPoint;
    public void SetHoldWalk() => targetPoint = holdWalkPoint;
    public void SetHoldAttack() => targetPoint = holdAttackPoint;
}
