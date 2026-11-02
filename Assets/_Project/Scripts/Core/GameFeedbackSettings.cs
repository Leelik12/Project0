using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Общие настройки обратной связи: звуки попаданий, трассеры, виньетка комфорта.
/// Один ассет лежит в Resources/GameFeedbackSettings и подхватывается из кода — в сценах ничего назначать не нужно.
/// Пустые звуки заменяются синтезированными (см. ProceduralSounds), так что всё работает и без клипов.
/// </summary>
[CreateAssetMenu(menuName = "Project0/Game Feedback Settings", fileName = "GameFeedbackSettings")]
public class GameFeedbackSettings : ScriptableObject
{
    private const string ResourcePath = "GameFeedbackSettings";
    private static GameFeedbackSettings instance;

    [Header("Звуки попаданий (пусто — синтезированный звук)")]
    public AudioClip hitClip;
    public AudioClip headshotClip;
    public AudioClip killClip;
    [Range(0f, 1f)] public float hitVolume = 0.5f;
    [Tooltip("Группа микшера для звуков обратной связи (например, громкость оружия)")]
    public AudioMixerGroup feedbackMixerGroup;

    [Header("Сандевистан (пусто — синтезированный звук)")]
    public AudioClip sandevistanStartClip;
    public AudioClip sandevistanEndClip;

    [Header("Трассеры")]
    [Tooltip("Шейдер трассеров. Ссылка из ассета гарантирует, что шейдер попадёт в билд.")]
    public Shader tracerShader;
    public Color playerTracerColor = new(1f, 0.8f, 0.4f, 0.9f);
    public Color enemyTracerColor = new(1f, 0.25f, 0.2f, 0.9f);
    public float tracerWidth = 0.012f;
    public float tracerLength = 2.5f;
    public float tracerSpeed = 250f;

    [Header("Комфорт")]
    [Tooltip("Префаб TunnelingVignette из XRI Starter Assets — сужение обзора при движении")]
    public GameObject tunnelingVignettePrefab;

    /// <summary>Ассет из Resources; если его нет — настройки по умолчанию.</summary>
    public static GameFeedbackSettings Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<GameFeedbackSettings>(ResourcePath);
                if (instance == null) instance = CreateInstance<GameFeedbackSettings>();
            }
            return instance;
        }
    }
}
