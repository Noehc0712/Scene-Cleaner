using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GameTimerHUD : MonoBehaviour
{
    private const string GameSceneName = "MapTestScene";

    [Header("Timer")]
    [SerializeField, Min(1f)] private float gameDuration = 30f;
    [SerializeField, Min(0f)] private float warningThreshold = 10f;

    [Header("Colors")]
    [SerializeField] private Color normalTextColor = new Color32(205, 232, 229, 255);
    [SerializeField] private Color warningTextColor = new Color32(235, 80, 75, 255);
    [SerializeField] private Color panelBackgroundColor = new(0.015f, 0.025f, 0.03f, 0.82f);

    [Header("Layout")]
    [SerializeField, Min(12f)] private float fontSize = 42f;
    [SerializeField] private Vector2 panelSize = new(230f, 82f);
    [SerializeField] private Vector2 topRightOffset = new(-38f, -32f);

    [Header("Scene UI")]
    [SerializeField] private TMP_Text timerText;

    private float remainingTime;
    private bool isRunning;
    private Color activeNormalTextColor;

    public static float RemainingTime { get; private set; } = 30f;
    public static bool HasTimeExpired { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadedCallback()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForInitialScene()
    {
        TryCreateInScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreateInScene(scene);
    }

    private static void TryCreateInScene(Scene scene)
    {
        if (scene.name != GameSceneName || FindAnyObjectByType<GameTimerHUD>() != null)
            return;

        GameObject timerObject = new(nameof(GameTimerHUD));
        SceneManager.MoveGameObjectToScene(timerObject, scene);
        timerObject.AddComponent<GameTimerHUD>();
    }

    private void Awake()
    {
        bool usesSceneText = timerText != null;
        if (!usesSceneText)
            CreateHUD();

        activeNormalTextColor = usesSceneText ? timerText.color : normalTextColor;
        ResetTimer();
    }

    private IEnumerator Start()
    {
        // Do not consume play time while the new scene is hidden by the fade overlay.
        while (SceneTransition.IsTransitioning)
            yield return null;

        isRunning = true;
    }

    private void Update()
    {
        if (!isRunning || HasTimeExpired)
            return;

        remainingTime = Mathf.Max(0f, remainingTime - Time.unscaledDeltaTime);
        RemainingTime = remainingTime;
        UpdateDisplay();

        if (remainingTime <= 0f)
        {
            HasTimeExpired = true;
            isRunning = false;
            UpdateDisplay();
        }
    }

    private void ResetTimer()
    {
        remainingTime = gameDuration;
        RemainingTime = gameDuration;
        HasTimeExpired = false;
        isRunning = false;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (timerText == null)
            return;

        int displaySeconds = Mathf.CeilToInt(remainingTime);
        int minutes = displaySeconds / 60;
        int seconds = displaySeconds % 60;
        timerText.text = $"{minutes:00}:{seconds:00}";
        timerText.color = remainingTime <= warningThreshold
            ? warningTextColor
            : activeNormalTextColor;
    }

    private void CreateHUD()
    {
        GameObject canvasObject = new(
            "Game Timer Canvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panelObject = new("Timer Panel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.one;
        panelRect.anchorMax = Vector2.one;
        panelRect.pivot = Vector2.one;
        panelRect.anchoredPosition = topRightOffset;
        panelRect.sizeDelta = panelSize;

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = panelBackgroundColor;
        panelImage.raycastTarget = false;

        GameObject textObject = new("Timer Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panelObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 6f);
        textRect.offsetMax = new Vector2(-12f, -6f);

        timerText = textObject.GetComponent<TMP_Text>();
        timerText.fontSize = fontSize;
        timerText.fontStyle = FontStyles.Bold;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.raycastTarget = false;
    }
}
