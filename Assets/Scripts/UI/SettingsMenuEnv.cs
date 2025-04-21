using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Audio;

public class SettingsMenuEnv : MonoBehaviour
{
    public Slider volumeSlider;
    public List<AudioSource> sfxAudioSources;
    public AudioMixer audioMixer;

    void Start()
    {
        // Устанавливаем значение слайдера из StaticHolder
        volumeSlider.value = StaticHolder.EnvVolume;
        SetVolume(StaticHolder.EnvVolume);
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    void OnVolumeChanged(float volume)
    {
        SetVolume(volume);
        StaticHolder.EnvVolume = volume; // Сохраняем значение
    }

    public void SetVolume(float volume)
    {
        // Значения громкости в микшере обычно от -80 до 0 дБ
        // Чтобы громкость не была логарифмической, оставляем линейную шкалу
        float dB = Mathf.Lerp(-30f, 10f, volume); // volume от 0 до 1
        audioMixer.SetFloat("EnvVolume", dB);

        // Сохраняем в StaticHolder, если нужно
        StaticHolder.EnvVolume = volume;
    }
}