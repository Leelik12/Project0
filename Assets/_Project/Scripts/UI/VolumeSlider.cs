using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>Слайдер громкости, привязанный к параметру AudioMixer. Наследники задают параметр и поле в GameState.</summary>
public abstract class VolumeSlider : MonoBehaviour
{
    private const float MuteDb = -80f; // минимум AudioMixer

    public Slider volumeSlider;
    public AudioMixer audioMixer;

    protected abstract string MixerParameter { get; }
    protected abstract float SavedVolume { get; set; }

    private void Start()
    {
        volumeSlider.SetValueWithoutNotify(SavedVolume);
        ApplyVolume(SavedVolume);
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    private void OnVolumeChanged(float volume)
    {
        SavedVolume = volume;
        ApplyVolume(volume);
    }

    // Громкость слышится логарифмически: 0.5 слайдера ≈ -6 dB, 1 — 0 dB (без усиления), 0 — тишина.
    private void ApplyVolume(float volume01)
    {
        float db = volume01 <= 0.0001f ? MuteDb : Mathf.Log10(volume01) * 20f;
        audioMixer.SetFloat(MixerParameter, db);
    }
}
