using UnityEngine;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public sealed class GameAudioManager : MonoBehaviour
{
    [Header("Background Music")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField, Range(0f, 1f)] private float backgroundMusicVolume = 0.25f;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip cctvStaticSound;
    [SerializeField] private AudioClip objectClickSound;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField, Range(0f, 1f)] private float soundEffectVolume = 0.7f;

    private static GameAudioManager instance;
    private AudioSource musicSource;
    private AudioSource cctvSource;
    private AudioSource soundEffectSource;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogError("GameAudioManager는 씬에 하나만 있어야 합니다.", this);
            enabled = false;
            return;
        }

        instance = this;
        musicSource = gameObject.AddComponent<AudioSource>();
        cctvSource = gameObject.AddComponent<AudioSource>();
        soundEffectSource = gameObject.AddComponent<AudioSource>();

        ConfigureSource(musicSource);
        ConfigureSource(cctvSource);
        ConfigureSource(soundEffectSource);
        musicSource.loop = true;
        musicSource.volume = backgroundMusicVolume;

        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    private static void ConfigureSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
    }

    public static void PlayCCTVStatic(float maximumDuration)
    {
        if (instance == null || instance.cctvStaticSound == null)
            return;

        instance.cctvSource.Stop();
        instance.cctvSource.clip = instance.cctvStaticSound;
        instance.cctvSource.loop = false;
        instance.cctvSource.volume = instance.soundEffectVolume;
        instance.cctvSource.Play();
        instance.cctvSource.SetScheduledEndTime(
            AudioSettings.dspTime + Mathf.Max(0.01f, maximumDuration));
    }

    public static void PlayObjectClick()
    {
        instance?.PlayEffect(instance.objectClickSound);
    }

    public static void PlayButtonClick()
    {
        instance?.PlayEffect(instance.buttonClickSound);
    }

    private void PlayEffect(AudioClip clip)
    {
        if (clip != null)
            soundEffectSource.PlayOneShot(clip, soundEffectVolume);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
