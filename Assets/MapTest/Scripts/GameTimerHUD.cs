using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GameTimerHUD : MonoBehaviour
{
    private const string GameSceneName =
        "MapTestScene";


    [Header("Timer")]

    [SerializeField, Min(1f)]
    private float gameDuration = 30f;

    [SerializeField, Min(0f)]
    private float warningThreshold = 10f;


    [Header("Colors")]

    [SerializeField]
    private Color normalTextColor =
        new Color32(205, 232, 229, 255);

    [SerializeField]
    private Color warningTextColor =
        new Color32(235, 80, 75, 255);

    [SerializeField]
    private Color panelBackgroundColor =
        new(0.015f, 0.025f, 0.03f, 0.82f);


    [Header("Layout")]

    [SerializeField, Min(12f)]
    private float fontSize = 42f;

    [SerializeField]
    private Vector2 panelSize =
        new(230f, 82f);

    [SerializeField]
    private Vector2 topRightOffset =
        new(-38f, -32f);


    [Header("Scene UI")]

    [SerializeField]
    private TMP_Text timerText;


    private float remainingTime;

    private bool isRunning;

    private Color activeNormalTextColor;


    // =============================================================
    // 외부에서 확인할 수 있는 타이머 상태
    // =============================================================

    public static float RemainingTime
    {
        get;
        private set;
    } = 30f;


    public static bool HasTimeExpired
    {
        get;
        private set;
    }


    public bool IsRunning
    {
        get
        {
            return isRunning;
        }
    }


    /// <summary>
    /// 타이머가 0초가 되었을 때 한 번 발생한다.
    ///
    /// 이후 GameFlowManager가 이 이벤트를 받아
    /// 라운드를 종료하게 된다.
    /// </summary>
    public event Action TimerExpired;


    // =============================================================
    // 씬 로딩
    // =============================================================

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad
    )]
    private static void RegisterSceneLoadedCallback()
    {
        SceneManager.sceneLoaded -=
            HandleSceneLoaded;

        SceneManager.sceneLoaded +=
            HandleSceneLoaded;
    }


    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.AfterSceneLoad
    )]
    private static void CreateForInitialScene()
    {
        TryCreateInScene(
            SceneManager.GetActiveScene()
        );
    }


    private static void HandleSceneLoaded(
        Scene scene,
        LoadSceneMode mode
    )
    {
        TryCreateInScene(
            scene
        );
    }


    private static void TryCreateInScene(
        Scene scene
    )
    {
        if (
            scene.name != GameSceneName ||
            FindAnyObjectByType<GameTimerHUD>() != null
        )
        {
            return;
        }


        GameObject timerObject =
            new GameObject(
                nameof(GameTimerHUD)
            );


        SceneManager.MoveGameObjectToScene(
            timerObject,
            scene
        );


        timerObject.AddComponent<GameTimerHUD>();
    }


    // =============================================================
    // Unity 생명주기
    // =============================================================

    private void Awake()
    {
        bool usesSceneText =
            timerText != null;


        if (!usesSceneText)
        {
            CreateHUD();
        }


        activeNormalTextColor =
            usesSceneText
                ? timerText.color
                : normalTextColor;


        /*
         * 게임 씬에 들어왔다고 해서
         * 타이머를 자동으로 시작하지 않는다.
         *
         * Gemini의 숨김 배치가 끝난 뒤
         * GameFlowManager가 StartTimer()를 호출한다.
         */
        ResetTimer();
    }


    private void Update()
    {
        if (!isRunning ||
            HasTimeExpired)
        {
            return;
        }


        remainingTime =
            Mathf.Max(
                0f,
                remainingTime -
                Time.unscaledDeltaTime
            );


        RemainingTime =
            remainingTime;


        UpdateDisplay();


        // =========================================================
        // 시간 종료
        // =========================================================

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;

            RemainingTime = 0f;

            HasTimeExpired = true;

            isRunning = false;


            UpdateDisplay();


            Debug.Log(
                "=== 라운드 타이머 종료 ==="
            );


            /*
             * GameFlowManager에게
             * 시간이 끝났다고 알려준다.
             */
            TimerExpired?.Invoke();
        }
    }


    // =============================================================
    // 타이머 제어
    // =============================================================

    /// <summary>
    /// 현재 남아 있는 시간부터
    /// 타이머를 시작한다.
    ///
    /// 일반적으로 ResetTimer() 후 호출한다.
    /// </summary>
    public void StartTimer()
    {
        if (isRunning)
        {
            Debug.LogWarning(
                "타이머가 이미 실행 중입니다."
            );

            return;
        }


        if (HasTimeExpired ||
            remainingTime <= 0f)
        {
            Debug.LogWarning(
                "타이머 시간이 이미 종료되었습니다. " +
                "먼저 ResetTimer()를 실행해 주세요."
            );

            return;
        }


        isRunning = true;


        Debug.Log(
            "=== 라운드 타이머 시작 ===\n" +
            $"시작 시간: {remainingTime:F1}초"
        );
    }


    /// <summary>
    /// 현재 시간을 유지한 상태로
    /// 카운트다운만 정지한다.
    /// </summary>
    public void StopTimer()
    {
        if (!isRunning)
        {
            return;
        }


        isRunning = false;


        Debug.Log(
            "=== 라운드 타이머 정지 ===\n" +
            $"남은 시간: {remainingTime:F1}초"
        );
    }


    /// <summary>
    /// 타이머를 라운드 기본 시간으로 되돌린다.
    ///
    /// Reset 후에는 자동 시작하지 않는다.
    /// </summary>
    public void ResetTimer()
    {
        remainingTime =
            gameDuration;


        RemainingTime =
            gameDuration;


        HasTimeExpired =
            false;


        isRunning =
            false;


        UpdateDisplay();
    }


    // =============================================================
    // UI 표시
    // =============================================================

    private void UpdateDisplay()
    {
        if (timerText == null)
        {
            return;
        }


        int displaySeconds =
            Mathf.CeilToInt(
                remainingTime
            );


        int minutes =
            displaySeconds / 60;


        int seconds =
            displaySeconds % 60;


        timerText.text =
            $"{minutes:00}:{seconds:00}";


        timerText.color =
            remainingTime <= warningThreshold
                ? warningTextColor
                : activeNormalTextColor;
    }


    // =============================================================
    // 자동 HUD 생성
    // =============================================================

    private void CreateHUD()
    {
        GameObject canvasObject =
            new GameObject(
                "Game Timer Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );


        canvasObject.transform.SetParent(
            transform,
            false
        );


        Canvas canvas =
            canvasObject.GetComponent<Canvas>();


        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;


        canvas.sortingOrder =
            50;


        CanvasScaler scaler =
            canvasObject
                .GetComponent<CanvasScaler>();


        scaler.uiScaleMode =
            CanvasScaler.ScaleMode
                .ScaleWithScreenSize;


        scaler.referenceResolution =
            new Vector2(
                1920f,
                1080f
            );


        scaler.matchWidthOrHeight =
            0.5f;


        GameObject panelObject =
            new GameObject(
                "Timer Panel",
                typeof(RectTransform),
                typeof(Image)
            );


        panelObject.transform.SetParent(
            canvasObject.transform,
            false
        );


        RectTransform panelRect =
            panelObject
                .GetComponent<RectTransform>();


        panelRect.anchorMin =
            Vector2.one;

        panelRect.anchorMax =
            Vector2.one;

        panelRect.pivot =
            Vector2.one;

        panelRect.anchoredPosition =
            topRightOffset;

        panelRect.sizeDelta =
            panelSize;


        Image panelImage =
            panelObject.GetComponent<Image>();


        panelImage.color =
            panelBackgroundColor;


        panelImage.raycastTarget =
            false;


        GameObject textObject =
            new GameObject(
                "Timer Text",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)
            );


        textObject.transform.SetParent(
            panelObject.transform,
            false
        );


        RectTransform textRect =
            textObject
                .GetComponent<RectTransform>();


        textRect.anchorMin =
            Vector2.zero;

        textRect.anchorMax =
            Vector2.one;

        textRect.offsetMin =
            new Vector2(
                12f,
                6f
            );

        textRect.offsetMax =
            new Vector2(
                -12f,
                -6f
            );


        timerText =
            textObject.GetComponent<TMP_Text>();


        timerText.fontSize =
            fontSize;


        timerText.fontStyle =
            FontStyles.Bold;


        timerText.alignment =
            TextAlignmentOptions.Center;


        timerText.raycastTarget =
            false;
    }


    // =============================================================
    // 개발 단계 테스트용
    // =============================================================

    [ContextMenu("타이머 시작")]
    private void StartTimerForTest()
    {
        StartTimer();
    }


    [ContextMenu("타이머 정지")]
    private void StopTimerForTest()
    {
        StopTimer();
    }


    [ContextMenu("타이머 30초로 초기화")]
    private void ResetTimerForTest()
    {
        ResetTimer();


        Debug.Log(
            "=== 라운드 타이머 초기화 ===\n" +
            $"남은 시간: {RemainingTime:F1}초"
        );
    }
}