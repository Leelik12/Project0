using UnityEngine;

public class XRBodyColliderSync : MonoBehaviour
{
    [Header("Collider")]
    [SerializeField] private CharacterController capsuleCollider;

    private void Reset()
    {
        capsuleCollider = GetComponent<CharacterController>();
    }

    private void LateUpdate()
    {
        capsuleCollider.center = new Vector3(0, capsuleCollider.center.y,0);
    }
}