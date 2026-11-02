using UnityEngine;

/// <summary>
/// Синтезированные звуки по умолчанию: работают, пока в GameFeedbackSettings не назначены свои клипы.
/// Генерируются один раз при первом обращении.
/// </summary>
public static class ProceduralSounds
{
    private const int SampleRate = 44100;

    private static AudioClip hit, headshot, kill, sandevistanStart, sandevistanEnd;

    /// <summary>Короткий сухой щелчок попадания.</summary>
    public static AudioClip Hit => hit ??= Create("Hit", 0.06f, t =>
        (Noise() * 0.5f + Sine(2600f, t) * 0.5f) * Envelope(t, 0.001f, 60f));

    /// <summary>Звонкий «динь» хедшота: несколько негармоничных обертонов, как у колокольчика.</summary>
    public static AudioClip Headshot => headshot ??= Create("Headshot", 0.35f, t =>
        (Sine(1900f, t) * 0.5f + Sine(2870f, t) * 0.3f + Sine(4120f, t) * 0.2f) * Envelope(t, 0.002f, 12f)
        + Noise() * 0.4f * Envelope(t, 0.001f, 120f));

    /// <summary>Убийство: низкий удар с падающим тоном и тихий звон сверху.</summary>
    public static AudioClip Kill => kill ??= Create("Kill", 0.4f, t =>
        Sine(Mathf.Lerp(140f, 50f, t / 0.4f), t) * Envelope(t, 0.002f, 9f)
        + Sine(1300f, t) * 0.25f * Envelope(t, 0.01f, 10f));

    /// <summary>Включение Сандевистана: тон уходит вниз, как будто время тормозит.</summary>
    public static AudioClip SandevistanStart => sandevistanStart ??= Create("SandevistanStart", 0.7f, t =>
        Sweep(900f, 120f, t, 0.7f) * (0.7f + 0.3f * Mathf.Sin(t * 60f)) * Envelope(t, 0.02f, 3f));

    /// <summary>Выключение Сандевистана: тон возвращается вверх.</summary>
    public static AudioClip SandevistanEnd => sandevistanEnd ??= Create("SandevistanEnd", 0.45f, t =>
        Sweep(120f, 900f, t, 0.45f) * Envelope(t, 0.02f, 5f));

    private static AudioClip Create(string name, float duration, System.Func<float, float> wave)
    {
        int length = Mathf.CeilToInt(duration * SampleRate);
        var data = new float[length];
        for (int i = 0; i < length; i++)
        {
            data[i] = Mathf.Clamp(wave((float)i / SampleRate), -1f, 1f) * 0.8f;
        }

        AudioClip clip = AudioClip.Create("Procedural" + name, length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static float Sine(float frequency, float t) => Mathf.Sin(2f * Mathf.PI * frequency * t);

    // Частота плавно меняется: фаза — интеграл частоты по времени
    private static float Sweep(float from, float to, float t, float duration)
    {
        float k = (to - from) / duration;
        return Mathf.Sin(2f * Mathf.PI * (from * t + 0.5f * k * t * t));
    }

    private static float Noise() => Random.value * 2f - 1f;

    // Быстрая атака и экспоненциальное затухание
    private static float Envelope(float t, float attack, float decay) =>
        Mathf.Clamp01(t / attack) * Mathf.Exp(-decay * t);
}
