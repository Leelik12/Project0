using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Настройки динамической музыки (A-01). Один ассет лежит в Resources/MusicSettings и подхватывается MusicDirector.
/// Если есть только один трек (mainClip), интенсивность строится из него: в спокойном режиме он тише и приглушён
/// фильтром низких частот, в бою звучит полностью. Если назначены calmClip и combatClip — они кроссфейдятся
/// (клипы должны быть одной длины и темпа: играют синхронно).
/// </summary>
[CreateAssetMenu(menuName = "Project0/Music Settings", fileName = "MusicSettings")]
public class MusicSettings : ScriptableObject
{
    [Header("Треки")]
    [Tooltip("Единственный трек: спокойный вариант получается фильтром и тише, боевой — полностью")]
    public AudioClip mainClip;
    [Tooltip("Необязательно: отдельный спокойный трек (вместе с combatClip)")]
    public AudioClip calmClip;
    [Tooltip("Необязательно: отдельный боевой трек (вместе с calmClip)")]
    public AudioClip combatClip;

    [Header("Вывод")]
    [Tooltip("Группа микшера «Environment» — слайдер громкости в настройках продолжает работать. Пусто — берётся у музыки сцены")]
    public AudioMixerGroup mixerGroup;
    [Range(0f, 1f)] public float baseVolume = 0.6f;

    [Header("Интенсивность")]
    [Tooltip("Секунд на плавный переход между спокойной и боевой музыкой")]
    public float crossfadeTime = 1.5f;
    [Tooltip("Сколько секунд после последней агрессии/попадания музыка остаётся боевой")]
    public float combatHoldTime = 6f;

    [Header("Спокойный режим (один трек)")]
    [Range(0f, 1f)] public float calmVolumeFactor = 0.55f;
    [Tooltip("Частота среза фильтра в спокойном режиме, Гц")]
    public float calmCutoff = 2200f;

    [Header("Пауза и Сандевистан")]
    [Range(0f, 1f)] public float pauseVolumeFactor = 0.3f;
    public float pauseCutoff = 900f;
    [Range(0.5f, 1f)] public float sandevistanPitch = 0.92f;
    public float pitchChangeSpeed = 1.5f;

    /// <summary>Спокойный вариант: отдельный клип или основной.</summary>
    public AudioClip CalmOrMain => calmClip != null ? calmClip : mainClip;

    /// <summary>Есть ли отдельная боевая дорожка (двухслойный режим).</summary>
    public bool HasCombatLayer => combatClip != null && CalmOrMain != null;
}
