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
        // Устанавливаем значение слайдера из StaticHolder
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
        // Значения громкости в микшере обычно от -80 до 0 дБ
        // Чтобы громкость не была логарифмической, оставляем линейную шкалу
        float dB = Mathf.Lerp(-30f, 10f, volume); // volume от 0 до 1
        audioMixer.SetFloat("GunVolume", dB);

        // Сохраняем в StaticHolder, если нужно
        StaticHolder.GunVolume = volume;
    }
}