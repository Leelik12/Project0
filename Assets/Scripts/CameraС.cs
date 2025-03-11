using UnityEngine;

public class CameraС : MonoBehaviour
{
    // Переменные под чувствительность по осям. Не целые.
    public float sensX;
    public float sensY;
    // Переменная под направление, хранит в себе полный набор координат
    public Transform orientation;
    // Переменные ткущего поворота, не целые
    float xRotation;
    float yRotation;
    // При начале работы скрипта
    private void Start()
    {
        // блокируем Курсор и делаем его невидимым
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    // Постоянно при работе
    private void Update()
    {
        //записываем координаты с мышки.
        //Не забываем про независимость от кадров и умножение на чувствительность.
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

        //Изменяем повороты осей и ограничиваем значение от -90 до 90 (float число)
        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        //Устанавливаем повороты при помощи создания направление при помощи Quaternion
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);
    }
}
