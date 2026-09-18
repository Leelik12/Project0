using UnityEngine;

/// <summary>
/// Подгоняет CharacterController под реальное положение головы в VR: высота и центр капсулы следуют за камерой.
/// </summary>
public class XRBodyColliderSync : MonoBehaviour
{
    [Header("XR Camera (обычно: XR Origin/Main Camera)")]
    [SerializeField] private Transform cameraTransform;

    [Header("Character Controller")]
    [SerializeField] private CharacterController characterController;

    [Header("Настройки Capsule")]
    [SerializeField] private float skinWidth = 0.05f;
    [SerializeField] private float minHeight = 0.5f;
    [SerializeField] private float maxHeight = 3.0f;
    [SerializeField] public GameObject PosBelt;

    private void Reset()
    {
        cameraTransform = Camera.main != null ? Camera.main.transform : null;
        characterController = GetComponent<CharacterController>();
    }

    private void LateUpdate()
    {
        if (cameraTransform == null || characterController == null) return;

        Vector3 localHeadPos = transform.InverseTransformPoint(cameraTransform.position);

        float newHeight = Mathf.Clamp(localHeadPos.y, minHeight, maxHeight);
        characterController.height = newHeight;
        characterController.center = new Vector3(localHeadPos.x, newHeight / 2f + skinWidth, localHeadPos.z);
    }
}
