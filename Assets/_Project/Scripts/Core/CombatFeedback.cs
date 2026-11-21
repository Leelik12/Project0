using UnityEngine;

/// <summary>
/// Звуковой отклик на бой: попадание, хедшот, убийство, Сандевистан. Создаётся сам при старте игры
/// (живёт между сценами) и слушает <see cref="GameEvents"/>. Звук двумерный — это «хитмаркер» для игрока,
/// а не звук в мире.
/// </summary>
public class CombatFeedback : MonoBehaviour
{
    private const float MinHitInterval = 0.04f; // дробь даёт много попаданий за кадр — один звук на залп

    private static CombatFeedback instance;

    private AudioSource source;
    private GameFeedbackSettings settings;
    private float lastHitSoundTime = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        var go = new GameObject(nameof(CombatFeedback));
        DontDestroyOnLoad(go);
        go.AddComponent<CombatFeedback>();
    }

    private void Awake()
    {
        instance = this;
        settings = GameFeedbackSettings.Instance;

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true; // звук включения паузы/Сандевистана не должен глохнуть
        source.outputAudioMixerGroup = settings.feedbackMixerGroup;

        GameEvents.EnemyHit += OnEnemyHit;
        GameEvents.SandevistanChanged += OnSandevistanChanged;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        GameEvents.EnemyHit -= OnEnemyHit;
        GameEvents.SandevistanChanged -= OnSandevistanChanged;
    }

    /// <summary>Звук попадания без события GameEvents — для мишеней тира, которые не считаются врагами.</summary>
    public static void PlayHit(bool headshot, bool killed)
    {
        if (instance != null) instance.PlayHitSound(headshot, killed);
    }

    private void OnEnemyHit(EnemyHitInfo hit) => PlayHitSound(hit.Headshot, hit.Killed);

    private void PlayHitSound(bool headshot, bool killed)
    {
        if (killed)
        {
            Play(settings.killClip != null ? settings.killClip : ProceduralSounds.Kill, settings.hitVolume);
            if (headshot) Play(settings.headshotClip != null ? settings.headshotClip : ProceduralSounds.Headshot, settings.hitVolume);
            return;
        }

        if (Time.unscaledTime - lastHitSoundTime < MinHitInterval) return;
        lastHitSoundTime = Time.unscaledTime;

        AudioClip clip = headshot
            ? (settings.headshotClip != null ? settings.headshotClip : ProceduralSounds.Headshot)
            : (settings.hitClip != null ? settings.hitClip : ProceduralSounds.Hit);
        Play(clip, settings.hitVolume);
    }

    private void OnSandevistanChanged(bool active)
    {
        AudioClip clip = active
            ? (settings.sandevistanStartClip != null ? settings.sandevistanStartClip : ProceduralSounds.SandevistanStart)
            : (settings.sandevistanEndClip != null ? settings.sandevistanEndClip : ProceduralSounds.SandevistanEnd);
        Play(clip, 0.7f);
    }

    private void Play(AudioClip clip, float volume)
    {
        if (clip != null) source.PlayOneShot(clip, volume);
    }
}
