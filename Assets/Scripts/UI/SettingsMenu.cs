using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Audio;

public class SettingsMenu : MonoBehaviour
{
    public Slider volumeSlider;
    public List<AudioSource> sfxAudioSources;
    public AudioMixer audioMixer;
    void Start()
    {
        volumeSlider.value = StaticHolder.GunVolume;
        SetVolume(StaticHolder.GunVolume);
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }
    void OnVolumeChanged(float volume)
    {
        SetVolume(volume);
        StaticHolder.GunVolume = volume; // Сохраняем значение
    }
    public void SetVolume(float volume)
    {
        float dB = Mathf.Lerp(-30f, 10f, volume); // volume от 0 до 1
        audioMixer.SetFloat("GunVolume", dB);
        StaticHolder.GunVolume = volume;
    }
}