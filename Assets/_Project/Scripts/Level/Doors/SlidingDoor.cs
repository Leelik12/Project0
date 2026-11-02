using UnityEngine;

/// <summary>
/// Дверь, которая один раз сдвигается по вертикали, когда игрок входит в триггер и выполнено условие CanOpen().
/// Наследники задают только условие; имена классов привязаны к сценам — не переименовывать.
/// </summary>
public abstract class SlidingDoor : MonoBehaviour
{
    [SerializeField] private float openHeight = 2.5f;   // смещение по Y (отрицательное — вниз)
    [SerializeField] private float openSpeed = 1f;
    [SerializeField] private AudioSource openSound;

    private Vector3 targetPosition;
    private bool isMoving;
    private bool hasOpened;
    private bool playerNearby;

    protected abstract bool CanOpen();

    private void Start()
    {
        targetPosition = transform.position + Vector3.up * openHeight;
    }

    private void Update()
    {
        // Условие проверяется каждый кадр, пока игрок рядом: оно может выполниться уже после входа в триггер
        // (например, апгрейд купили, стоя у двери магазина).
        if (!hasOpened && playerNearby && CanOpen())
        {
            hasOpened = true;
            isMoving = true;
            if (openSound != null) openSound.Play();
        }

        if (!isMoving) return;

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, openSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            isMoving = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) playerNearby = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerNearby = false;
    }
}
