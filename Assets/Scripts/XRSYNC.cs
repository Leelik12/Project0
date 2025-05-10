using UnityEngine;

public class XRBodyColliderSync : MonoBehaviour
{
    [Header("XR Camera (обычно: XR Origin/Main Camera)")]
    [SerializeField] private Transform cameraTransform;

    [Header("Character Controller")]
    [SerializeField] private CharacterController characterController; // Используем CharacterController

    [Header("Настройки Capsule")]
    [SerializeField] private float skinWidth = 0.05f; // отступ от пола
    [SerializeField] public GameObject PosBelt;

    private void Reset()
    {
        cameraTransform = Camera.main?.transform;
        characterController = GetComponent<CharacterController>(); // Получаем CharacterController
    }

    private void LateUpdate()
    {
        if (cameraTransform == null || characterController == null)
        {
            Debug.LogWarning("CameraTransform или CharacterController не назначены.");
            return;
        }

        // Лог глобальных позиций камеры и объекта
        Debug.Log($"[DEBUG] cameraTransform.position = {cameraTransform.position}");
        Debug.Log($"[DEBUG] XR Origin (this.transform.position) = {transform.position}");

        // Получаем локальную позицию головы относительно XR Origin
        Vector3 localHeadPos = transform.InverseTransformPoint(cameraTransform.position);
        Debug.Log($"[DEBUG] localHeadPos = {localHeadPos}");

        // Обновляем высоту CharacterController (с учетом skin width)
        float newHeight = Mathf.Clamp(localHeadPos.y, 0.5f, 3.0f);
        characterController.height = newHeight;
        characterController.center = new Vector3(localHeadPos.x, newHeight / 2f + skinWidth, localHeadPos.z);
        //PosBelt.transform.position = new Vector3(0, 0.5f, 0);
        Debug.Log($"[DEBUG] CharacterController height = {characterController.height}, center = {characterController.center}");
    }
}
