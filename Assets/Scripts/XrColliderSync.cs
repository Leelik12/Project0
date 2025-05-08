using UnityEngine;

public class XRBodyColliderSync : MonoBehaviour
{
    [Header("Collider")]
    [SerializeField] private CharacterController capsuleCollider;
    [SerializeField] public GameObject Locomotion;
    private void Reset()
    {
        capsuleCollider = GetComponent<CharacterController>();
    }

    private void LateUpdate()
    {
        capsuleCollider.center = new Vector3(Locomotion.transform.localPosition.x, capsuleCollider.center.y, Locomotion.transform.localPosition.z);
    }
}