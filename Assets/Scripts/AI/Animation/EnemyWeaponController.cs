using UnityEngine;

public class EnemyWeaponController : MonoBehaviour
{
    public Transform weapon;
    public Transform holdIdlePoint;
    public Transform holdRunPoint;
    public Transform holdWalkPoint;
    public Transform holdAttackPoint;

    private Transform targetPoint; // Куда хотим прийти
    private float smoothTime = 0.2f; // Время сглаживания
    private Vector3 velocity = Vector3.zero; // для сглаживания позиции

    private void Update()
    {
        if (targetPoint != null && weapon != null)
        {
            weapon.position = Vector3.SmoothDamp(weapon.position, targetPoint.position, ref velocity, smoothTime);
            weapon.rotation = Quaternion.Lerp(weapon.rotation, targetPoint.rotation, Time.deltaTime * 10f);
        }
    }

    public void SetHoldIdle()
    {
        targetPoint = holdIdlePoint;
    }

    public void SetHoldRun()
    {
        targetPoint = holdRunPoint;
    }
    public void SetHoldWalk()
    {
        targetPoint = holdWalkPoint;
    }

    public void SetHoldAttack()
    {
        targetPoint = holdAttackPoint;
    }
}
