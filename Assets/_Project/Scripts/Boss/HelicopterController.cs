using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Финальный босс — вертолёт. Активируется триггером: вылетает к игроку, кружит вокруг него и стреляет очередями,
/// при смерти улетает и запускает экран победы.
/// </summary>
public class HelicopterController : MonoBehaviour
{
    public Transform player;
    public Transform helicopter;
    public GameObject hel;                    // корневой объект вертолёта (уничтожается после побега)
    public HelicopterGunSystem gunSystem;
    public Transform exitPoint;
    public Transform escapePoint;             // куда улетает перед смертью
    public AudioSource audioSource;
    public EnemyHealth health;

    public float flyOutSpeed = 5f;
    public float approachDistance = 10f;      // на каком расстоянии от игрока зависает
    public float circleSpeed = 20f;           // градусов в секунду

    public float minDirectionChangeTime = 15f;
    public float maxDirectionChangeTime = 30f;
    public float delayAfterDeath = 5f;

    private const float HoverHeight = 10f;
    private const float TurnSpeed = 2f;

    private bool isActivated;
    private bool isCircling;
    private bool isDefeated;
    private int circleDirection = 1;

    private void OnTriggerEnter(Collider other)
    {
        if (isActivated || !other.CompareTag("Player")) return;

        isActivated = true;
        StartCoroutine(HelicopterSequence());
    }

    private void Update()
    {
        if (!isActivated || isDefeated) return;

        if (health != null && health.GetCurrentHp() <= 0f)
        {
            isDefeated = true;
            StartCoroutine(EscapeAndDestroy());
            StartCoroutine(DelayedVictory());
            return;
        }

        if (isCircling)
        {
            helicopter.RotateAround(player.position, Vector3.up, circleSpeed * circleDirection * Time.deltaTime);
            Vector3 dirToPlayer = player.position - Vector3.up - helicopter.position;
            helicopter.rotation = Quaternion.Slerp(helicopter.rotation, GetCorrectedLookRotation(dirToPlayer), TurnSpeed * Time.deltaTime);
        }
    }

    private IEnumerator HelicopterSequence()
    {
        if (audioSource != null)
        {
            audioSource.loop = true;
            audioSource.Play();
        }

        // Вылет к точке над крышей рядом с игроком
        Vector3 targetPos = player.position + player.forward * approachDistance + Vector3.up * HoverHeight;
        yield return MoveTo(targetPos, flyOutSpeed, 0.5f);

        // Разворот к игроку
        yield return RotateTo(GetCorrectedLookRotation(player.position - helicopter.position));

        if (gunSystem != null) gunSystem.ActivateWeapons();

        isCircling = true;
        StartCoroutine(ChangeDirectionRoutine());
    }

    private IEnumerator ChangeDirectionRoutine()
    {
        while (isCircling)
        {
            yield return new WaitForSeconds(Random.Range(minDirectionChangeTime, maxDirectionChangeTime));
            circleDirection *= -1;
        }
    }

    private IEnumerator EscapeAndDestroy()
    {
        isCircling = false;
        if (gunSystem != null) gunSystem.DeactivateWeapons();

        Vector3 target = escapePoint.position;
        yield return RotateTo(GetCorrectedLookRotation(target - helicopter.position));
        yield return MoveTo(target, flyOutSpeed * 1.5f, 1f);

        if (audioSource != null) audioSource.Stop();
        Destroy(hel);
    }

    private IEnumerator DelayedVictory()
    {
        yield return new WaitForSeconds(delayAfterDeath);
        GameState.GameOver = true; // в меню покажется экран победы
        GameState.ResetRun();
        SceneManager.LoadSceneAsync(0);
    }

    private IEnumerator MoveTo(Vector3 target, float speed, float tolerance)
    {
        while (Vector3.Distance(helicopter.position, target) > tolerance)
        {
            helicopter.position = Vector3.MoveTowards(helicopter.position, target, speed * Time.deltaTime);
            yield return null;
        }
    }

    private IEnumerator RotateTo(Quaternion target)
    {
        while (Quaternion.Angle(helicopter.rotation, target) > 1f)
        {
            helicopter.rotation = Quaternion.Slerp(helicopter.rotation, target, TurnSpeed * Time.deltaTime);
            yield return null;
        }
    }

    // Нос модели смотрит по +X, поэтому LookRotation доворачиваем на -90° вокруг Y
    private static Quaternion GetCorrectedLookRotation(Vector3 direction)
    {
        return Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90f, 0);
    }
}
