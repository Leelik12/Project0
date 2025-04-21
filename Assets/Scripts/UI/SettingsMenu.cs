using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public AudioMixer audioMixer; // подключи MainAudioMixer
    public Slider sfxSlider; // подключи слайдер из Canvas

    void Start()
    {
        // Загрузить сохранённую громкость при старте
        if (PlayerPrefs.HasKey("GunVolume"))
        {
            float volume = PlayerPrefs.GetFloat("GunVolume");
            sfxSlider.value = volume;
            SetVolume(volume);
        }

        sfxSlider.onValueChanged.AddListener(SetVolume);
    }

    public void SetVolume(float volume)
    {
        // от 0 до 1 — линейное значение
        float db = (volume == 0f) ? -80f : Mathf.Lerp(-40f, 0f, volume); // -40dB = почти неслышно, 0dB = макс
        audioMixer.SetFloat("GunVolume", db);
    }
}
