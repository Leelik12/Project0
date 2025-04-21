using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SFXVolumeManager : MonoBehaviour
{
    public Slider volumeSlider;
    public List<AudioSource> sfxAudioSources;

    private const string VolumePrefKey = "GunVolume";

    void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat(VolumePrefKey, 1f); // по умолчанию 1 (макс громкость)
        volumeSlider.value = savedVolume;
        SetVolume(savedVolume);
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    void OnVolumeChanged(float volume)
    {
        SetVolume(volume);
        PlayerPrefs.SetFloat(VolumePrefKey, volume);
        PlayerPrefs.Save();
    }

    void SetVolume(float volume)
    {
        foreach (var source in sfxAudioSources)
        {
            source.volume = volume;
        }
    }
}
