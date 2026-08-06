using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CCTVCameraHUD : MonoBehaviour
{
    private const string GameSceneName = "MapTestScene";

    [Header("Display")]
    [SerializeField] private string labelFormat = "CCTV {0:00}";

    [Header("Scene UI")]
    [SerializeField] private TMP_Text cameraText;

    [Header("Runtime Fallback")]
    [SerializeField] private Color fallbackTextColor = new Color32(190, 220, 216, 255);
    [SerializeField, Min(12f)] private float fallbackFontSize = 30f;

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
        if (scene.name != GameSceneName || FindAnyObjectByType<CCTVCameraHUD>() != null)
            return;

        GameObject hudObject = new(nameof(CCTVCameraHUD));
        SceneManager.MoveGameObjectToScene(hudObject, scene);
        hudObject.AddComponent<CCTVCameraHUD>();
    }

    private void Awake()
    {
        if (cameraText == null)
            CreateFallbackUI();
    }

    private void OnEnable()
    {
        CCTVSwitcher.ActiveCameraChanged += UpdateCameraNumber;
        UpdateCameraNumber(Mathf.Max(1, CCTVSwitcher.CurrentCameraNumber));
    }

    private void OnDisable()
    {
        CCTVSwitcher.ActiveCameraChanged -= UpdateCameraNumber;
    }

    private void UpdateCameraNumber(int cameraNumber)
    {
        if (cameraText != null && cameraNumber > 0)
            cameraText.text = string.Format(labelFormat, cameraNumber);
    }

    private void CreateFallbackUI()
    {
        GameObject canvasObject = new(
            "CCTV Indicator Canvas",
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

        GameObject textObject = new("CCTV Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(40f, 34f);
        rect.sizeDelta = new Vector2(260f, 60f);

        cameraText = textObject.GetComponent<TMP_Text>();
        cameraText.fontSize = fallbackFontSize;
        cameraText.fontStyle = FontStyles.Bold;
        cameraText.alignment = TextAlignmentOptions.MidlineLeft;
        cameraText.color = fallbackTextColor;
        cameraText.raycastTarget = false;
    }
}
