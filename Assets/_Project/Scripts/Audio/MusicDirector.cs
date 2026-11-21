using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>
/// Динамическая музыка (A-01). Создаётся сам при старте игры и живёт между сценами, поэтому трек не обрывается
/// при переходе меню - лобби - этажи. В бою (кто-то из врагов в агре/атаке, недавнее попадание или урон по игроку,
/// босс-вертолёт жив) музыка плавно переходит в полную; в спокойном режиме она тише и приглушена.
/// Пауза приглушает музыку, Сандевистан слегка опускает тон. Идёт через группу «Environment» микшера —
/// слайдер громкости в настройках работает как раньше.
/// Музыку сцены (объект «Music» с тем же клипом) директор забирает себе и выключает её источник.
/// </summary>
public class MusicDirector : MonoBehaviour
{
    private const string SettingsPath = "MusicSettings";
    private const string SceneMusicName = "Music";
    private const int LastHubScene = GameState.UpgradeShopScene; // меню (0) и лобби (1): музыка звучит полностью
    private const float OpenCutoff = 22000f;
    private const float EnemyScanInterval = 0.25f;
    private const float AgroHold = 4f; // враг на секунду потерял игрока — музыка не должна дёргаться

    private sealed class Layer
    {
        public AudioSource Source;
        public AudioLowPassFilter Filter;
    }

    /// <summary>Текущая интенсивность 0..1 (для отладки и тестов).</summary>
    public static MusicDirector Instance { get; private set; }
    public float Intensity => intensity;

    private MusicSettings settings;
    private Layer layerA;       // спокойный/основной трек
    private Layer layerB;       // боевой трек (только в двухслойном режиме)
    private float baseVolume;
    private float intensity;    // 0 — спокойно, 1 — бой
    private float combatUntil;  // до какого момента (unscaledTime) держится боевой режим
    private float nextScan;
    private bool paused;
    private bool sandevistan;
    private float pitch = 1f;
    private float pauseBlend;   // 0..1 плавное приглушение на паузе
    private HelicopterController boss;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (Instance != null) return;

        var go = new GameObject(nameof(MusicDirector));
        DontDestroyOnLoad(go);
        go.AddComponent<MusicDirector>();
    }

    private void Awake()
    {
        settings = Resources.Load<MusicSettings>(SettingsPath);
        if (settings == null || settings.CalmOrMain == null)
        {
            Destroy(gameObject); // нет настроек или трека — музыки нет, как и раньше
            return;
        }

        Instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
        GameEvents.EnemyHit += OnEnemyHit;
        GameEvents.PlayerDamaged += OnPlayerDamaged;
        GameEvents.SandevistanChanged += OnSandevistanChanged;
        GameEvents.PauseChanged += OnPauseChanged;

        AudioMixerGroup group = settings.mixerGroup;
        baseVolume = settings.baseVolume;
        AdoptSceneMusic(ref group);

        layerA = CreateLayer("Calm", settings.CalmOrMain, group);
        if (settings.HasCombatLayer) layerB = CreateLayer("Combat", settings.combatClip, group);
        StartLayers();

        boss = FindFirstObjectByType<HelicopterController>();
        intensity = IsHubScene() ? 1f : 0f;
        ApplyMix();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GameEvents.EnemyHit -= OnEnemyHit;
        GameEvents.PlayerDamaged -= OnPlayerDamaged;
        GameEvents.SandevistanChanged -= OnSandevistanChanged;
        GameEvents.PauseChanged -= OnPauseChanged;
    }

    private Layer CreateLayer(string layerName, AudioClip clip, AudioMixerGroup group)
    {
        var go = new GameObject(layerName);
        go.transform.SetParent(transform, false);

        var source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true; // на паузе музыка не замолкает, а приглушается
        source.outputAudioMixerGroup = group;

        var filter = go.AddComponent<AudioLowPassFilter>();
        filter.cutoffFrequency = OpenCutoff;
        filter.enabled = false;
        return new Layer { Source = source, Filter = filter };
    }

    private void StartLayers()
    {
        layerA.Source.Play();
        if (layerB != null) layerB.Source.Play(); // оба слоя стартуют вместе и идут синхронно
    }

    // Музыка сцены (LobbyScene/«Music») отдаётся директору: берём её микшерную группу, источник гасим
    private void AdoptSceneMusic(ref AudioMixerGroup group)
    {
        foreach (AudioSource source in FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!source.enabled || source.transform.IsChildOf(transform) || source.name != SceneMusicName || source.clip != settings.CalmOrMain) continue;

            if (group == null) group = source.outputAudioMixerGroup;
            source.Stop();
            source.enabled = false;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AudioMixerGroup group = layerA.Source.outputAudioMixerGroup;
        AdoptSceneMusic(ref group);

        boss = FindFirstObjectByType<HelicopterController>();
        combatUntil = 0f;
        paused = false;
        sandevistan = false;
        if (!layerA.Source.isPlaying) StartLayers();
    }

    // ---------- События ----------

    private void OnEnemyHit(EnemyHitInfo hit) => HoldCombat(settings.combatHoldTime);
    private void OnPlayerDamaged(float damage) => HoldCombat(settings.combatHoldTime);
    private void OnSandevistanChanged(bool active) => sandevistan = active;
    private void OnPauseChanged(bool value) => paused = value;

    private void HoldCombat(float seconds) => combatUntil = Mathf.Max(combatUntil, Time.unscaledTime + seconds);

    // ---------- Обновление ----------

    private void Update()
    {
        float now = Time.unscaledTime;
        if (now >= nextScan)
        {
            nextScan = now + EnemyScanInterval;
            if (AnyEnemyAlerted()) HoldCombat(AgroHold);
        }

        float target = IsHubScene() || BossActive() || now < combatUntil ? 1f : 0f;
        float fadeSpeed = settings.crossfadeTime > 0.01f ? 1f / settings.crossfadeTime : 100f;
        intensity = Mathf.MoveTowards(intensity, target, fadeSpeed * Time.unscaledDeltaTime);
        pauseBlend = Mathf.MoveTowards(pauseBlend, paused ? 1f : 0f, 4f * Time.unscaledDeltaTime);
        pitch = Mathf.MoveTowards(pitch, sandevistan ? settings.sandevistanPitch : 1f, settings.pitchChangeSpeed * Time.unscaledDeltaTime);

        ApplyMix();
    }

    private static bool IsHubScene() => SceneManager.GetActiveScene().buildIndex <= LastHubScene;

    private bool BossActive()
    {
        return boss != null && boss.isActiveAndEnabled && (boss.health == null || !boss.health.IsDead);
    }

    private static bool AnyEnemyAlerted()
    {
        var enemies = EnemyManager.AllEnemies;
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyStateManager enemy = enemies[i];
            if (enemy == null) continue;
            if (enemy.CurrentState == enemy.AgroState || enemy.CurrentState == enemy.AttackState) return true;
        }
        return false;
    }

    private void ApplyMix()
    {
        float duck = Mathf.Lerp(1f, settings.pauseVolumeFactor, pauseBlend);
        float volume = baseVolume * duck;
        float cutoffDuck = Mathf.Lerp(OpenCutoff, settings.pauseCutoff, pauseBlend);

        if (layerB != null)
        {
            // Два слоя: кроссфейд с постоянной громкостью (по корню), фильтр нужен только паузе
            layerA.Source.volume = volume * Mathf.Sqrt(1f - intensity);
            layerB.Source.volume = volume * Mathf.Sqrt(intensity);
            SetCutoff(layerA, cutoffDuck);
            SetCutoff(layerB, cutoffDuck);
            layerB.Source.pitch = pitch;
        }
        else
        {
            // Один трек: спокойно — тише и глуше, бой — полностью
            layerA.Source.volume = volume * Mathf.Lerp(settings.calmVolumeFactor, 1f, intensity);
            float calmCutoff = Mathf.Exp(Mathf.Lerp(Mathf.Log(settings.calmCutoff), Mathf.Log(OpenCutoff), intensity));
            SetCutoff(layerA, Mathf.Min(calmCutoff, cutoffDuck));
        }
        layerA.Source.pitch = pitch;
    }

    private static void SetCutoff(Layer layer, float cutoff)
    {
        bool needed = cutoff < OpenCutoff - 1f;
        if (layer.Filter.enabled != needed) layer.Filter.enabled = needed;
        if (needed) layer.Filter.cutoffFrequency = cutoff;
    }
}
