using System.Collections;
using UnityEngine;

public class HelicopterController : MonoBehaviour
{
    public Transform player;                  // Ссылка на игрока
    public Transform helicopter;              // Ссылка на вертолёт
    public Transform exitPoint;               // Точка, из-под которой вылетает вертолёт

    public float flyOutSpeed = 5f;            // Скорость вылета
    public float approachDistance = 10f;      // На каком расстоянии вертолёт начинает кружить
    public float circleSpeed = 20f;           // Скорость вращения вокруг игрока (градусов в секунду)

    public float minDirectionChangeTime = 15f; // Мин. интервал смены направления
    public float maxDirectionChangeTime = 30f; // Макс. интервал смены направления

    public float tiltAngle = 20f;             // Угол наклона к игроку
    public float tiltSpeed = 2f;              // Скорость наклона

    public GameObject bulletPrefab;           // Префаб пули
    public float bulletSpeed = 30f;           // Скорость пули
    public float fireInterval = 2f;           // Интервал стрельбы (сек)

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

        // Плавно наклоняемся лицом к игроку
        Quaternion targetRotation = Quaternion.LookRotation(player.position - helicopter.position);
        targetRotation *= Quaternion.Euler(tiltAngle, 0, 0);  // Добавляем наклон вниз

        while (Quaternion.Angle(helicopter.rotation, targetRotation) > 1f)
        {
            helicopter.rotation = Quaternion.Slerp(helicopter.rotation, targetRotation, tiltSpeed * Time.deltaTime);
            yield return null;
        }

        // Запускаем кружение и стрельбу
        isCircling = true;
        StartCoroutine(ChangeDirectionRoutine());
        StartCoroutine(FireRoutine());
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

    IEnumerator FireRoutine()
    {
        while (isCircling)
        {
            FireAtPlayer();
            yield return new WaitForSeconds(fireInterval);
        }
    }

    void FireAtPlayer()
    {
        if (bulletPrefab != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, helicopter.position, Quaternion.identity);
            Vector3 dir = (player.position - helicopter.position).normalized;
            bullet.GetComponent<Rigidbody>().velocity = dir * bulletSpeed;
        }
    }

    void Update()
    {
        if (isCircling)
        {
            helicopter.RotateAround(player.position, Vector3.up, circleSpeed * circleDirection * Time.deltaTime);

            // Вертолёт всегда смотрит на игрока с наклоном
            Vector3 dirToPlayer = player.position - helicopter.position;
            Quaternion lookRotation = Quaternion.LookRotation(dirToPlayer);
            lookRotation *= Quaternion.Euler(tiltAngle, 0, 0);
            helicopter.rotation = Quaternion.Slerp(helicopter.rotation, lookRotation, tiltSpeed * Time.deltaTime);
        }
    }
}
