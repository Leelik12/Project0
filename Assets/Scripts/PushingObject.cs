using UnityEngine;

public class PushingObject : MonoBehaviour
{
    [SerializeField] GameObject _object;

    private void OnCollisionEnter(Collision collision)
    {
        _object.transform.position += new Vector3(0, 0, 1f);
    }
}
