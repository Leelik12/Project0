using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class HelicopterController : MonoBehaviour
{
    public Transform player;                  // Ссылка на игрока
    public Transform helicopter;
    public GameObject hel; // Ссылка на вертолёт (родительский объект)
    public HelicopterGunSystem gunSystem;     // Система стрельбы
    public Transform exitPoint;               // Точка, из-под которой вылетает вертолёт
    public Transform escapePoint;             // Точка куда улетит вертолёт перед смертью
    public AudioSource audioSource;
    public EnemyHeaths health;

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
        if (audioSource != null)
        {
            audioSource.loop = true;
            audioSource.Play();
        }

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

        if (gunSystem != null)
        {
            gunSystem.ActivateWeapons();
        }

        // Запускаем кружение
        isCircling = true;

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
        if (!isActivated) return;

        // Проверка смерти
        if (health != null && health.GetCurrentHp() <= 0f)
        {
            StartCoroutine(EscapeAndDestroy());
            isActivated = false; // Чтобы не запускался снова
            return;
        }

        if (isCircling)
        {
            helicopter.RotateAround(player.position, Vector3.up, circleSpeed * circleDirection * Time.deltaTime);

            Vector3 dirToPlayer = player.position - helicopter.position;
            Quaternion lookRotation = GetCorrectedLookRotation(dirToPlayer);
            helicopter.rotation = Quaternion.Slerp(helicopter.rotation, lookRotation, 2f * Time.deltaTime);
        }
    }

    IEnumerator EscapeAndDestroy()
    {
        isCircling = false;

        if (gunSystem != null)
        {
            gunSystem.DeactivateWeapons();
        }

        Vector3 target = escapePoint.position;

        // Разворачиваем вертолет в сторону точки побега
        Vector3 directionToEscape = target - helicopter.position;
        Quaternion targetRotation = GetCorrectedLookRotation(directionToEscape);

        // Плавно разворачиваем вертолет в нужное направление
        while (Quaternion.Angle(helicopter.rotation, targetRotation) > 1f)
        {
            helicopter.rotation = Quaternion.Slerp(helicopter.rotation, targetRotation, 2f * Time.deltaTime);
            yield return null;
        }

        // Перемещаем вертолет в точку побега
        while (Vector3.Distance(helicopter.position, target) > 1f)
        {
            helicopter.position = Vector3.MoveTowards(helicopter.position, target, flyOutSpeed * 1.5f * Time.deltaTime);
            yield return null;
        }

        // Останавливаем звук, если он был
        if (audioSource != null)
            audioSource.Stop();

        // Уничтожаем вертолет
        Destroy(hel);
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
