using UnityEngine;

public class OpenDoorLeft : MonoBehaviour
{
    [SerializeField] GameObject _door;
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _door.transform.position += new Vector3(0, 0, -1f);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _door.transform.position += new Vector3(0, 0, 1f);
    }
}
