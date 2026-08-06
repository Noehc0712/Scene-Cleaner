using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class SceneTransition : MonoBehaviour
{
    private const float DefaultFadeOutDuration = 2.5f;
    private const float DefaultFadeInDuration = 2.5f;
    private const int OverlaySortingOrder = 32767;

    private static SceneTransition instance;

    private CanvasGroup overlay;
    private bool isTransitioning;

    public static bool IsTransitioning => instance != null && instance.isTransitioning;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateAutomatically()
    {
        if (instance != null)
            return;

        GameObject transitionObject = new(nameof(SceneTransition));
        instance = transitionObject.AddComponent<SceneTransition>();
        DontDestroyOnLoad(transitionObject);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        CreateOverlay();
    }

    public static bool LoadScene(
        string sceneName,
        float fadeOutDuration = DefaultFadeOutDuration,
        float fadeInDuration = DefaultFadeInDuration)
    {
        if (instance == null || instance.isTransitioning || string.IsNullOrWhiteSpace(sceneName))
            return false;

        instance.StartCoroutine(instance.LoadSceneRoutine(
            sceneName,
            Mathf.Max(0f, fadeOutDuration),
            Mathf.Max(0f, fadeInDuration)));
        return true;
    }

    private IEnumerator LoadSceneRoutine(
        string sceneName,
        float fadeOutDuration,
        float fadeInDuration)
    {
        isTransitioning = true;
        overlay.blocksRaycasts = true;
        overlay.interactable = true;

        yield return Fade(0f, 1f, fadeOutDuration);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        if (loadOperation == null)
        {
            Debug.LogError($"Scene transition failed: '{sceneName}' could not be loaded.", this);
            yield return Fade(1f, 0f, fadeInDuration);
            FinishTransition();
            yield break;
        }

        while (!loadOperation.isDone)
            yield return null;

        // Give the newly loaded scene one frame to finish its initial layout.
        yield return null;
        yield return Fade(1f, 0f, fadeInDuration);
        FinishTransition();
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            overlay.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        overlay.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            overlay.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, progress));
            yield return null;
        }

        overlay.alpha = to;
    }

    private void FinishTransition()
    {
        overlay.alpha = 0f;
        overlay.blocksRaycasts = false;
        overlay.interactable = false;
        isTransitioning = false;
    }

    private void CreateOverlay()
    {
        GameObject canvasObject = new(
            "Scene Transition Canvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = OverlaySortingOrder;

        overlay = canvasObject.GetComponent<CanvasGroup>();
        overlay.alpha = 0f;
        overlay.blocksRaycasts = false;
        overlay.interactable = false;

        GameObject fadeImageObject = new("Fade", typeof(RectTransform), typeof(Image));
        fadeImageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform fadeRect = fadeImageObject.GetComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = Vector2.zero;
        fadeRect.offsetMax = Vector2.zero;

        Image fadeImage = fadeImageObject.GetComponent<Image>();
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = true;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
