using UnityEngine;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public sealed class GameAudioManager : MonoBehaviour
{
    private const string BackgroundMusicVolumeKey = "SceneCleaner.BackgroundMusicVolume";
    private const string SoundEffectVolumeKey = "SceneCleaner.SoundEffectVolume";
    private const float DefaultBackgroundMusicVolume = 0.25f;
    private const float DefaultSoundEffectVolume = 0.7f;

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

    public static float BackgroundMusicVolume => instance != null
        ? instance.backgroundMusicVolume
        : PlayerPrefs.GetFloat(BackgroundMusicVolumeKey, DefaultBackgroundMusicVolume);

    public static float SoundEffectVolume => instance != null
        ? instance.soundEffectVolume
        : PlayerPrefs.GetFloat(SoundEffectVolumeKey, DefaultSoundEffectVolume);

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            instance.MergeMissingClipsFrom(this);
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        musicSource = gameObject.AddComponent<AudioSource>();
        cctvSource = gameObject.AddComponent<AudioSource>();
        soundEffectSource = gameObject.AddComponent<AudioSource>();

        ConfigureSource(musicSource);
        ConfigureSource(cctvSource);
        ConfigureSource(soundEffectSource);
        musicSource.loop = true;

        backgroundMusicVolume = PlayerPrefs.GetFloat(
            BackgroundMusicVolumeKey, backgroundMusicVolume);
        soundEffectVolume = PlayerPrefs.GetFloat(
            SoundEffectVolumeKey, soundEffectVolume);
        musicSource.volume = backgroundMusicVolume;

        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    public static void ConfigurePersistentAudio(
        AudioClip music,
        AudioClip buttonClick,
        float musicVolume,
        float effectVolume)
    {
        if (instance == null)
        {
            GameObject audioObject = new(nameof(GameAudioManager));
            instance = audioObject.AddComponent<GameAudioManager>();
        }

        instance.backgroundMusicVolume = PlayerPrefs.GetFloat(
            BackgroundMusicVolumeKey, Mathf.Clamp01(musicVolume));
        instance.soundEffectVolume = PlayerPrefs.GetFloat(
            SoundEffectVolumeKey, Mathf.Clamp01(effectVolume));

        if (buttonClick != null)
            instance.buttonClickSound = buttonClick;

        if (music == null)
            return;

        instance.backgroundMusic = music;
        instance.musicSource.volume = instance.backgroundMusicVolume;

        if (instance.musicSource.clip == music && instance.musicSource.isPlaying)
            return;

        instance.musicSource.clip = music;
        instance.musicSource.loop = true;
        instance.musicSource.Play();
    }

    public static void SetBackgroundMusicVolume(float volume)
    {
        float clampedVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(BackgroundMusicVolumeKey, clampedVolume);
        PlayerPrefs.Save();

        if (instance == null)
            return;

        instance.backgroundMusicVolume = clampedVolume;
        instance.musicSource.volume = clampedVolume;
    }

    public static void SetSoundEffectVolume(float volume)
    {
        float clampedVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SoundEffectVolumeKey, clampedVolume);
        PlayerPrefs.Save();

        if (instance != null)
            instance.soundEffectVolume = clampedVolume;
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

    public static void PlayCustomEffect(AudioClip clip)
    {
        instance?.PlayEffect(clip);
    }

    private void PlayEffect(AudioClip clip)
    {
        if (clip != null)
            soundEffectSource.PlayOneShot(clip, soundEffectVolume);
    }

    private void MergeMissingClipsFrom(GameAudioManager other)
    {
        if (backgroundMusic == null)
            backgroundMusic = other.backgroundMusic;

        if (cctvStaticSound == null)
            cctvStaticSound = other.cctvStaticSound;

        if (objectClickSound == null)
            objectClickSound = other.objectClickSound;

        if (buttonClickSound == null)
            buttonClickSound = other.buttonClickSound;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
