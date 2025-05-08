using UnityEngine;

public class XRBodyColliderSync : MonoBehaviour
{
    [Header("Collider")]
    [SerializeField] private CharacterController capsuleCollider;
    [SerializeField] public GameObject Locomotion;
    [SerializeField] public GameObject PosCamers;
    [SerializeField] public GameObject PosBelt;
    private void Reset()
    {
        capsuleCollider = GetComponent<CharacterController>();
    }

    private void LateUpdate()
    {
        //Locomotion.transform.rotation = PosCamers.transform.rotation;
        //capsuleCollider.transform.rotation = Locomotion.transform.rotation;
        Locomotion.transform.localPosition = new Vector3(-PosCamers.transform.localPosition.x, capsuleCollider.center.y, PosCamers.transform.localPosition.z);
        capsuleCollider.center = new Vector3(-Locomotion.transform.localPosition.x, capsuleCollider.center.y, Locomotion.transform.localPosition.z);
        PosBelt.transform.localPosition = new Vector3(-Locomotion.transform.localPosition.x, PosBelt.transform.localPosition.y, Locomotion.transform.localPosition.z);
    }
}