using UnityEngine;

public class HelicopterBoss : MonoBehaviour
{
    public Transform player; // Игрок
    public float rotationSpeed = 0.2f; // Скорость вращения (уменьшена для плавности)
    public float radius = 10f; // Радиус кругового движения
    public float riseHeight = 30f; // Насколько высоко вылететь из-под здания
    public float riseSpeed = 4f; // Скорость взлёта
    public float approachSpeed = 2f; // Скорость подлета к игроку

    private float angle = 0f; // Начальный угол вращения
    private bool isActivated = false; // Включение босса
    private bool isRising = false; // Взлет
    private bool isApproachingPlayer = false; // Подлетание к игроку
    private bool isRotating = false; // Начало вращения

    private Vector3 targetRisePosition; // Цель для взлета
    private Vector3 targetPosition; // Позиция на радиусе от игрока, где будет начато вращение

    private void Update()
    {
        if (!isActivated) return;

        if (isRising)
        {
            RiseFromCover(); // Взлет
        }
        else if (isApproachingPlayer)
        {
            ApproachToRadius(); // Подлет к радиусу вращения
        }
        else if (isRotating)
        {
            RotateAroundPlayer(); // Вращение вокруг игрока
        }
    }

    private void RiseFromCover()
    {
        // Плавно поднимаем вертолет на определенную высоту
        transform.position = Vector3.MoveTowards(transform.position, targetRisePosition, riseSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetRisePosition) < 0.1f)
        {
            // Когда достигли нужной высоты, начинаем подлет к радиусу
            isRising = false;
            isApproachingPlayer = true;
        }
    }

    private void ApproachToRadius()
    {
        // Позиция, которая находится на радиусе от игрока на высоте riseHeight
        targetPosition = new Vector3(player.position.x + Mathf.Cos(angle) * radius,
                                      player.position.y + riseHeight,
                                      player.position.z + Mathf.Sin(angle) * radius);

        // Плавно подлетаем к точке на радиусе
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, approachSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            // Когда достигли нужной позиции на радиусе, начинаем вращение
            isApproachingPlayer = false;
            isRotating = true;
        }
    }

    private void RotateAroundPlayer()
    {
        // Плавно вращаем вертолет вокруг игрока на заданном радиусе
        angle += rotationSpeed * Time.deltaTime; // медленное вращение

        // Рассчитываем новую позицию вертолета по кругу на радиусе
        float x = player.position.x + Mathf.Cos(angle) * radius;
        float z = player.position.z + Mathf.Sin(angle) * radius;
        float y = transform.position.y; // Высота остаётся постоянной

        // Обновляем позицию вертолета
        transform.position = new Vector3(x, y, z);

        // Плавно поворачиваем вертолет в сторону игрока
        Vector3 direction = player.position - transform.position;

        // Направляем вертолет лицом к игроку (смотрим на игрока)
        Quaternion targetRotation = Quaternion.LookRotation(direction); // Ожидаемое вращение к игроку

        // Плавно поворачиваем вертолет
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    public void ActivateBoss()
    {
        // Настроим стартовую позицию для взлета
        targetRisePosition = new Vector3(transform.position.x, transform.position.y + riseHeight, transform.position.z);
        isActivated = true;
        isRising = true; // Начинаем взлет
    }
}
