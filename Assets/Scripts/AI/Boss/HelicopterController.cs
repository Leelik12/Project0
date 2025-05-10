using System.Collections;
using UnityEngine;

public class HelicopterController : MonoBehaviour
{
    public Transform player;                  // Ссылка на игрока
    public Transform helicopter;              // Ссылка на вертолёт (родительский объект)
    public Transform exitPoint;               // Точка, из-под которой вылетает вертолёт
    public HelicopterWeaponSystem weaponSystem; // ссылка на скрипт оружия

    public float flyOutSpeed = 5f;            // Скорость вылета
    public float approachDistance = 10f;      // На каком расстоянии вертолёт начинает кружить
    public float circleSpeed = 20f;           // Скорость вращения вокруг игрока (градусов в секунду)

    public float minDirectionChangeTime = 15f; // Мин. интервал смены направления
    public float maxDirectionChangeTime = 30f; // Макс. интервал смены направления

    private bool isActivated = false;
    private bool isCircling = false;
    private int circleDirection = 1;          // 1 или -1

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isActivated)
        {
            isActivated = true;
            StartCoroutine(HelicopterSequence());
        }
    }

    IEnumerator HelicopterSequence()
    {
        // Вылет из под здания к точке над крышей (рядом с игроком)
        Vector3 targetPos = player.position + (player.forward * approachDistance) + Vector3.up * 5f;
        while (Vector3.Distance(helicopter.position, targetPos) > 0.5f)
        {
            helicopter.position = Vector3.MoveTowards(helicopter.position, targetPos, flyOutSpeed * Time.deltaTime);
            yield return null;
        }

        // Плавно разворачиваем вертолёт лицом к игроку
        Quaternion targetRotation = GetCorrectedLookRotation(player.position - helicopter.position);
        while (Quaternion.Angle(helicopter.rotation, targetRotation) > 1f)
        {
            helicopter.rotation = Quaternion.Slerp(helicopter.rotation, targetRotation, 2f * Time.deltaTime);
            yield return null;
        }

        // Запускаем кружение и стрельбу
        isCircling = true;

        if (weaponSystem != null)
        {
            weaponSystem.ActivateWeapons(); // Запускаем стрельбу
        }

        StartCoroutine(ChangeDirectionRoutine());
    }

    IEnumerator ChangeDirectionRoutine()
    {
        while (isCircling)
        {
            float waitTime = Random.Range(minDirectionChangeTime, maxDirectionChangeTime);
            yield return new WaitForSeconds(waitTime);
            circleDirection *= -1;
        }
    }
    void Update()
    {
        if (isCircling)
        {
            helicopter.RotateAround(player.position, Vector3.up, circleSpeed * circleDirection * Time.deltaTime);

            // Вертолёт всегда смотрит на игрока (ровно)
            Vector3 dirToPlayer = player.position - helicopter.position;
            Quaternion lookRotation = GetCorrectedLookRotation(dirToPlayer);
            helicopter.rotation = Quaternion.Slerp(helicopter.rotation, lookRotation, 2f * Time.deltaTime);
        }
    }

    // Эта функция корректирует поворот с учётом того, что нос модели по X+
    Quaternion GetCorrectedLookRotation(Vector3 direction)
    {
        Quaternion baseRotation = Quaternion.LookRotation(direction);
        // Поворачиваем на -90° вокруг Y, чтобы forward стал по X (а не по Z)
        baseRotation *= Quaternion.Euler(0, -90f, 0);
        return baseRotation;
    }
}
