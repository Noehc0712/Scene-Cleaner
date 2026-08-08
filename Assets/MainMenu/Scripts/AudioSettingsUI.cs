using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class AudioSettingsUI : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider backgroundMusicSlider;
    [SerializeField] private Slider soundEffectSlider;

    [Header("Optional Value Labels")]
    [SerializeField] private TMP_Text backgroundMusicValueText;
    [SerializeField] private TMP_Text soundEffectValueText;

    private void Awake()
    {
        if (backgroundMusicSlider == null || soundEffectSlider == null)
        {
            Debug.LogError("Audio Settings UI에 BGM과 효과음 Slider를 연결해 주세요.", this);
            enabled = false;
            return;
        }

        ConfigureSlider(backgroundMusicSlider);
        ConfigureSlider(soundEffectSlider);
        backgroundMusicSlider.onValueChanged.AddListener(SetBackgroundMusicVolume);
        soundEffectSlider.onValueChanged.AddListener(SetSoundEffectVolume);
    }

    private void OnEnable()
    {
        if (backgroundMusicSlider == null || soundEffectSlider == null)
            return;

        float musicVolume = GameAudioManager.BackgroundMusicVolume;
        float effectVolume = GameAudioManager.SoundEffectVolume;

        backgroundMusicSlider.SetValueWithoutNotify(musicVolume);
        soundEffectSlider.SetValueWithoutNotify(effectVolume);
        UpdateValueText(backgroundMusicValueText, musicVolume);
        UpdateValueText(soundEffectValueText, effectVolume);
    }

    private void SetBackgroundMusicVolume(float volume)
    {
        GameAudioManager.SetBackgroundMusicVolume(volume);
        UpdateValueText(backgroundMusicValueText, volume);
    }

    private void SetSoundEffectVolume(float volume)
    {
        GameAudioManager.SetSoundEffectVolume(volume);
        UpdateValueText(soundEffectValueText, volume);
    }

    private static void ConfigureSlider(Slider slider)
    {
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
    }

    private static void UpdateValueText(TMP_Text text, float volume)
    {
        if (text != null)
            text.text = $"{Mathf.RoundToInt(volume * 100f)}%";
    }

    private void OnDestroy()
    {
        if (backgroundMusicSlider != null)
            backgroundMusicSlider.onValueChanged.RemoveListener(SetBackgroundMusicVolume);

        if (soundEffectSlider != null)
            soundEffectSlider.onValueChanged.RemoveListener(SetSoundEffectVolume);
    }
}
