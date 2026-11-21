using UnityEngine;

/// <summary>
/// Мишень тира в лобби (C-01). Коллайдеры «Head» и «Body» на дочерних объектах — VRGun находит эту мишень сам
/// и считает хедшот по тегу. Это не враг: нет EnemyHealth, поэтому попадания не создают события GameEvents,
/// не дают эдди и не попадают в статистику забега — только в счёт тира (RangeLane).
/// «Убитая» мишень откидывается назад вокруг основания и через несколько секунд встаёт обратно.
/// </summary>
public class RangeTarget : MonoBehaviour
{
    private enum State { Up, Falling, Down, Rising }

    private const float WobbleDecay = 9f;

    [SerializeField] private float health = 100f;
    [Tooltip("Секунд лежит после попадания, прежде чем встанет")]
    [SerializeField] private float resetDelay = 4f;
    [SerializeField] private float fallTime = 0.25f;
    [SerializeField] private float riseTime = 0.45f;
    [Tooltip("На какой угол откидывается назад (вокруг локальной оси X)")]
    [SerializeField] private float fallAngle = 90f;
    [Tooltip("Кнопка «Сброс»: попадание обнуляет счёт тира, а не наносит урон")]
    [SerializeField] private bool resetsScore;

    private RangeLane lane;
    private Quaternion uprightRotation;
    private float currentHealth;
    private State state = State.Up;
    private float stateTime;
    private float wobble; // качание от попаданий, градусы

    public bool IsUp => state == State.Up;

    private void Awake()
    {
        uprightRotation = transform.localRotation;
        currentHealth = health;
        enabled = false; // Update нужен только пока мишень качается или падает
    }

    private void OnEnable()
    {
        if (lane == null) lane = RangeLane.Current;
    }

    /// <summary>Попадание пули (вызывает VRGun). Лежащую мишень игнорируем.</summary>
    public void Hit(float damage, bool headshot, Vector3 point, bool countsAsHit)
    {
        if (lane == null) lane = RangeLane.Current;
        if (resetsScore)
        {
            if (lane != null) lane.ResetScore();
            wobble = 8f;
            enabled = true;
            return;
        }
        if (state != State.Up) return;

        currentHealth -= damage;
        bool killed = currentHealth <= 0f;
        if (lane != null)
        {
            lane.RegisterHit(damage, headshot, killed, countsAsHit);
            lane.ShowDamage(point, damage, headshot);
        }
        CombatFeedback.PlayHit(headshot, killed);

        if (killed) SetState(State.Falling);
        else wobble = Mathf.Min(wobble + (headshot ? 7f : 4f), 14f);
        enabled = true;
    }

    private void SetState(State value)
    {
        state = value;
        stateTime = 0f;
    }

    private void Update()
    {
        stateTime += Time.deltaTime;
        wobble = Mathf.MoveTowards(wobble, 0f, WobbleDecay * Time.deltaTime);

        float tilt = 0f; // доля падения: 0 — стоит, 1 — лежит
        switch (state)
        {
            case State.Up:
                if (wobble <= 0f) enabled = false;
                break;
            case State.Falling:
                tilt = Mathf.SmoothStep(0f, 1f, stateTime / fallTime);
                if (stateTime >= fallTime) SetState(State.Down);
                break;
            case State.Down:
                tilt = 1f;
                if (stateTime >= resetDelay) SetState(State.Rising);
                break;
            case State.Rising:
                tilt = 1f - Mathf.SmoothStep(0f, 1f, stateTime / riseTime);
                if (stateTime >= riseTime)
                {
                    currentHealth = health;
                    SetState(State.Up);
                }
                break;
        }

        float angle = -fallAngle * tilt - wobble * Mathf.Sin(stateTime * 40f); // знак: назад от стрелка
        transform.localRotation = uprightRotation * Quaternion.Euler(angle, 0f, 0f);
    }
}
