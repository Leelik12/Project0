using UnityEngine;

public class CloseLiftDoors : MonoBehaviour
{
    [SerializeField] private float openHeight = -8f;
    [SerializeField] private float openSpeed = 1f;
    [SerializeField] private AudioSource openSound;

    private bool isOpening = false;
    private Vector3 targetPosition;
    private bool hasOpened = false;

    private void Start()
    {
        targetPosition = transform.position + Vector3.up * openHeight;
    }

    private void Update()
    {
        if (isOpening)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, openSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                isOpening = false; // остановка анимации после завершения
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasOpened && other.CompareTag("Player"))
        {
            isOpening = true;
            hasOpened = true;

            if (openSound != null)
            {
                openSound.Play();
            }
            Debug.Log("Дверь закрывается");
        }

    }
}
