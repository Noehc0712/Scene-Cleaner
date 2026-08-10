using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MainMenuUI : MonoBehaviour
{
    private const string GameSceneName = "MapTestScene";

    [Header("Menu Audio")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip pageTurnSound;
    [SerializeField, Range(0f, 1f)] private float backgroundMusicVolume = 0.25f;
    [SerializeField, Range(0f, 1f)] private float buttonClickVolume = 0.7f;

    [Header("Panels")]
    [SerializeField] private GameObject descriptionPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField, Range(0f, 1f)] private float settingsBackdropOpacity = 0.88f;

    [Header("Game Description")]
    [SerializeField] private Texture descriptionPaperTexture;

    [Header("Menu Cursor")]
    [SerializeField] private Texture2D menuCursor;
    [SerializeField] private Vector2 menuCursorHotspot;

    private void Awake()
    {
        GameAudioManager.ConfigurePersistentAudio(
            backgroundMusic,
            buttonClickSound,
            backgroundMusicVolume,
            buttonClickVolume);

        Cursor.SetCursor(
            menuCursor,
            menuCursorHotspot,
            CursorMode.Auto);

        ConfigureGameDescription();
    }

    private void ConfigureGameDescription()
    {
        if (descriptionPanel == null)
            return;

        GameDescriptionUI descriptionUI =
            descriptionPanel.GetComponent<GameDescriptionUI>();
        if (descriptionUI == null)
            descriptionUI = descriptionPanel.AddComponent<GameDescriptionUI>();

        descriptionUI.Configure(
            descriptionPaperTexture,
            pageTurnSound,
            ClosePanels);
    }

    public void StartGame()
    {
        GameAudioManager.PlayButtonClick();
        SceneTransition.LoadScene(GameSceneName);
    }

    public void OpenDescription()
    {
        GameAudioManager.PlayButtonClick();
        if (descriptionPanel != null &&
            descriptionPanel.TryGetComponent(out GameDescriptionUI descriptionUI))
        {
            descriptionUI.ShowFirstPage();
        }

        SetOnlyPanel(descriptionPanel);
    }

    public void OpenSettings()
    {
        GameAudioManager.PlayButtonClick();
        ApplySettingsBackdropOpacity();
        SetOnlyPanel(settingsPanel);
    }

    public void ClosePanels()
    {
        GameAudioManager.PlayButtonClick();
        SetOnlyPanel(null);
    }

    private void SetOnlyPanel(GameObject panelToOpen)
    {
        if (descriptionPanel != null)
            descriptionPanel.SetActive(descriptionPanel == panelToOpen);

        if (settingsPanel != null)
            settingsPanel.SetActive(settingsPanel == panelToOpen);
    }

    private void ApplySettingsBackdropOpacity()
    {
        if (settingsPanel == null || !settingsPanel.TryGetComponent(out Image backdrop))
            return;

        Color color = backdrop.color;
        color.a = settingsBackdropOpacity;
        backdrop.color = color;
    }
}
