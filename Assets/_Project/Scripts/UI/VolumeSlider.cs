using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>Слайдер громкости, привязанный к параметру AudioMixer. Наследники задают параметр и поле в GameState.</summary>
public abstract class VolumeSlider : MonoBehaviour
{
    private const float MinDb = -30f;
    private const float MaxDb = 10f;

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

    private void ApplyVolume(float volume01)
    {
        audioMixer.SetFloat(MixerParameter, Mathf.Lerp(MinDb, MaxDb, volume01));
    }
}
