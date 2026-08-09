using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class GameFlowManager : MonoBehaviour
{
    /*
     * 게임 규칙
     */
    private const int TotalClueCount = 4;
    private const int MaximumSearchAttempts = 5;


    [Header("게임 시작 설정")]
    [SerializeField]
    private bool autoStart = true;


    [Header("AI")]
    [SerializeField]
    private GeminiHidePlanner geminiHidePlanner;


    [Header("라운드 초기화")]
    [SerializeField]
    private AIRoundResetController roundResetController;


    [Header("타이머")]
    [SerializeField]
    private GameTimerHUD gameTimerHUD;


    [Header("플레이어 행동 기록")]
    [SerializeField]
    private PlayerActionRecorder playerActionRecorder;

    [SerializeField]
    private PlayerCameraRecorder playerCameraRecorder;

    [SerializeField]
    private PlayerHoverRecorder playerHoverRecorder;

    [SerializeField]
    private PlayerBehaviorSummary playerBehaviorSummary;


    [Header("플레이어 조사 입력")]
    [SerializeField]
    private CCTVCursorInteractor cctvCursorInteractor;


    [Header("AI 대기 화면")]
    [SerializeField]
    private GameObject aiLoadingPanel;

    [SerializeField]
    private TMP_Text aiLoadingText;


    /*
     * ================================
     * 1라운드 결과 화면
     * ================================
     */
    [Header("1라운드 결과 화면")]
    [SerializeField]
    private GameObject roundOneResultPanel;

    [SerializeField]
    private TMP_Text roundOneClueCountText;

    [SerializeField]
    private TMP_Text roundOneAttemptsText;

    [SerializeField]
    private Button nextRoundButton;


    /*
     * ================================
     * 최종 결과 화면
     * ================================
     */
    [Header("최종 결과 화면")]
    [SerializeField]
    private GameObject finalResultPanel;

    [SerializeField]
    private TMP_Text finalClueCountText;

    [SerializeField]
    private TMP_Text finalAttemptsText;

    [SerializeField]
    private TMP_Text finalStrategySummaryText;


    /*
     * ================================
     * 현재 게임 상태
     * ================================
     */
    [Header("현재 게임 상태")]
    [SerializeField]
    private int currentRound = 1;

    [SerializeField]
    private bool isWaitingForAI;

    [SerializeField]
    private bool isRoundPlaying;

    [SerializeField]
    private bool isWaitingForRoundTwoStart;

    [SerializeField]
    private bool isGameFinished;


    /*
     * ================================
     * 각 라운드 결과
     * ================================
     */
    [Header("라운드 결과")]
    [SerializeField]
    private int roundOneFoundClues;

    [SerializeField]
    private int roundOneUsedAttempts;

    [SerializeField]
    private int roundTwoFoundClues;

    [SerializeField]
    private int roundTwoUsedAttempts;


    /*
     * ================================
     * 행동 데이터 원본
     * ================================
     */
    [Header("라운드 행동 데이터")]
    [TextArea(5, 15)]
    [SerializeField]
    private string roundOneBehaviorSnapshot;

    [TextArea(5, 15)]
    [SerializeField]
    private string roundTwoBehaviorSnapshot;



    public int CurrentRound
    {
        get
        {
            return currentRound;
        }
    }


    public bool IsWaitingForAI
    {
        get
        {
            return isWaitingForAI;
        }
    }


    public bool IsRoundPlaying
    {
        get
        {
            return isRoundPlaying;
        }
    }


    public bool IsGameFinished
    {
        get
        {
            return isGameFinished;
        }
    }



    private void Awake()
    {
        /*
         * 필요한 게임 컴포넌트를 찾는다.
         */
        FindRequiredComponents();


        /*
         * 결과 화면은 게임 시작 시
         * 반드시 꺼져 있어야 한다.
         */
        HideAllResultScreens();


        /*
         * 다음 라운드 버튼은
         * 코드에서 직접 이벤트를 연결한다.
         *
         * 따라서 Inspector의 On Click에는
         * 따로 함수를 연결하지 않아도 된다.
         */
        if (nextRoundButton != null)
        {
            nextRoundButton.onClick.RemoveListener(
                ProceedToRoundTwo
            );

            nextRoundButton.onClick.AddListener(
                ProceedToRoundTwo
            );
        }


        /*
         * 게임이 시작되기 전에는
         * 플레이어가 CCTV 장소를 조사하지 못하게 한다.
         */
        SetPlayerInteractionEnabled(
            false
        );


        /*
         * 타이머도 시작 전 상태로 초기화한다.
         */
        if (gameTimerHUD != null)
        {
            gameTimerHUD.ResetTimer();
        }
    }



    private void OnEnable()
    {
        SubscribeEvents();
    }



    private void OnDisable()
    {
        UnsubscribeEvents();
    }



    private void OnDestroy()
    {
        /*
         * 버튼 이벤트 정리.
         */
        if (nextRoundButton != null)
        {
            nextRoundButton.onClick.RemoveListener(
                ProceedToRoundTwo
            );
        }
    }



    private void Start()
    {
        if (autoStart)
        {
            StartGame();
        }
    }



    /*
     * Inspector 연결이 빠진 경우에도
     * Scene 안에서 주요 게임 컴포넌트를 찾아준다.
     *
     * 결과 UI는 각각 역할이 다른 GameObject/TMP_Text이므로
     * Inspector에서 직접 연결한다.
     */
    private void FindRequiredComponents()
    {
        if (geminiHidePlanner == null)
        {
            geminiHidePlanner =
                FindFirstObjectByType<GeminiHidePlanner>();
        }


        if (roundResetController == null)
        {
            roundResetController =
                FindFirstObjectByType<AIRoundResetController>();
        }


        if (gameTimerHUD == null)
        {
            gameTimerHUD =
                FindFirstObjectByType<GameTimerHUD>();
        }


        if (playerActionRecorder == null)
        {
            playerActionRecorder =
                FindFirstObjectByType<PlayerActionRecorder>();
        }


        if (playerCameraRecorder == null)
        {
            playerCameraRecorder =
                FindFirstObjectByType<PlayerCameraRecorder>();
        }


        if (playerHoverRecorder == null)
        {
            playerHoverRecorder =
                FindFirstObjectByType<PlayerHoverRecorder>();
        }


        if (playerBehaviorSummary == null)
        {
            playerBehaviorSummary =
                FindFirstObjectByType<PlayerBehaviorSummary>();
        }


        if (cctvCursorInteractor == null)
        {
            cctvCursorInteractor =
                FindFirstObjectByType<CCTVCursorInteractor>();
        }
    }



    /*
     * Gemini 성공 이벤트와
     * 타이머 종료 이벤트를 연결한다.
     */
    private void SubscribeEvents()
    {
        if (geminiHidePlanner != null)
        {
            geminiHidePlanner.HidePlanSucceeded -=
                HandleHidePlanSucceeded;

            geminiHidePlanner.HidePlanSucceeded +=
                HandleHidePlanSucceeded;
        }


        if (gameTimerHUD != null)
        {
            gameTimerHUD.TimerExpired -=
                HandleTimerExpired;

            gameTimerHUD.TimerExpired +=
                HandleTimerExpired;
        }
    }



    private void UnsubscribeEvents()
    {
        if (geminiHidePlanner != null)
        {
            geminiHidePlanner.HidePlanSucceeded -=
                HandleHidePlanSucceeded;
        }


        if (gameTimerHUD != null)
        {
            gameTimerHUD.TimerExpired -=
                HandleTimerExpired;
        }
    }



    /*
     * ==========================================
     * 새로운 게임 시작
     * ==========================================
     */
    public void StartGame()
    {
        if (isWaitingForAI || isRoundPlaying)
        {
            Debug.LogWarning(
                "이미 게임이 진행 중입니다."
            );

            return;
        }


        Debug.Log(
            "====================================\n" +
            "=== SceneCleaner GAME START ===\n" +
            "===================================="
        );


        /*
         * 게임 상태 초기화.
         */
        currentRound = 1;

        isWaitingForAI = true;
        isRoundPlaying = false;
        isWaitingForRoundTwoStart = false;
        isGameFinished = false;


        /*
         * 라운드 결과 초기화.
         */
        roundOneFoundClues = 0;
        roundOneUsedAttempts = 0;

        roundTwoFoundClues = 0;
        roundTwoUsedAttempts = 0;


        /*
         * 행동 데이터 초기화.
         */
        roundOneBehaviorSnapshot =
            string.Empty;

        roundTwoBehaviorSnapshot =
            string.Empty;


        /*
         * 이전 결과 화면이 남아 있다면 제거.
         */
        HideAllResultScreens();


        /*
         * 새 게임이므로
         * 이전 플레이 기록은 모두 삭제한다.
         */
        ClearAllBehaviorRecords();


        /*
         * 기록기의 현재 라운드를
         * 1라운드로 지정한다.
         */
        if (playerActionRecorder != null)
        {
            playerActionRecorder.SetCurrentRound(
                1
            );
        }


        if (playerHoverRecorder != null)
        {
            playerHoverRecorder.SetCurrentRound(
                1
            );
        }


        /*
         * Gemini가 단서를 숨기는 동안
         * 플레이어 조사 입력 차단.
         */
        SetPlayerInteractionEnabled(
            false
        );


        /*
         * Gemini 성공 전까지
         * 타이머는 시작하지 않는다.
         */
        if (gameTimerHUD != null)
        {
            gameTimerHUD.ResetTimer();
        }


        /*
         * 1라운드 AI 대기 화면.
         *
         * 현재 currentRound == 1이므로:
         *
         * "AI가 단서를 숨기고 있습니다..."
         */
        ShowAILoadingScreen();


        Debug.Log(
            "=== ROUND 1 Gemini 배치 요청 시작 ==="
        );


        StartCoroutine(
            RequestRoundOneHidePlan()
        );
    }



    /*
     * Gemini에게 1라운드 배치를 요청한다.
     */
    private IEnumerator RequestRoundOneHidePlan()
    {
        if (geminiHidePlanner == null)
        {
            Debug.LogError(
                "GeminiHidePlanner가 없습니다."
            );

            yield break;
        }


        yield return
            geminiHidePlanner
                .RequestHidePlanForRound(
                    1
                );
    }



    /*
     * ==========================================
     * Gemini 배치 성공
     * ==========================================
     */
    private void HandleHidePlanSucceeded()
    {
        Debug.Log(
            $"=== ROUND {currentRound} Gemini 배치 성공 ==="
        );


        isWaitingForAI =
            false;


        /*
         * 2라운드 Gemini 요청이 성공했다면
         * Gemini는 이미 1라운드 행동 데이터를
         * 읽은 상태이다.
         *
         * 따라서 이 시점부터
         * 2라운드 행동 기록을 새로 시작하기 위해
         * 기록기를 초기화한다.
         */
        if (currentRound == 2)
        {
            PrepareRecordersForRoundTwo();
        }


        /*
         * AI 대기 화면 종료.
         */
        HideAILoadingScreen();


        /*
         * 실제 플레이 시작.
         */
        StartRoundGameplay();
    }



    /*
     * ==========================================
     * 실제 라운드 플레이 시작
     * ==========================================
     */
    private void StartRoundGameplay()
    {
        isRoundPlaying =
            true;


        isWaitingForRoundTwoStart =
            false;


        /*
         * 플레이어 조사 허용.
         */
        SetPlayerInteractionEnabled(
            true
        );


        /*
         * 타이머 시작.
         */
        if (gameTimerHUD != null)
        {
            gameTimerHUD.StartTimer();
        }


        /*
         * CCTV 체류 시간 기록 시작.
         */
        if (playerCameraRecorder != null)
        {
            playerCameraRecorder.StartRecording();
        }


        /*
         * HideZone Hover 시간 기록 시작.
         */
        if (playerHoverRecorder != null)
        {
            playerHoverRecorder.StartRecording();
        }


        Debug.Log(
            "====================================\n" +
            $"=== ROUND {currentRound} START ===\n" +
            "타이머 시작\n" +
            "플레이어 행동 기록 시작\n" +
            "===================================="
        );
    }



    /*
     * GameTimerHUD가 0초가 되면 호출.
     */
    private void HandleTimerExpired()
    {
        if (!isRoundPlaying)
        {
            return;
        }


        EndCurrentRound();
    }



    /*
     * ==========================================
     * 현재 라운드 종료
     * ==========================================
     */
    private void EndCurrentRound()
    {
        isRoundPlaying =
            false;


        /*
         * 시간 종료 후에는
         * 더 이상 장소를 조사할 수 없다.
         */
        SetPlayerInteractionEnabled(
            false
        );


        /*
         * CCTV 체류 시간 기록 종료.
         */
        if (
            playerCameraRecorder != null &&
            playerCameraRecorder.IsRecording
        )
        {
            playerCameraRecorder.StopRecording();
        }


        /*
         * Hover 기록 종료.
         */
        if (
            playerHoverRecorder != null &&
            playerHoverRecorder.IsRecording
        )
        {
            playerHoverRecorder.StopRecording();
        }


        /*
         * 현재 라운드의 행동 데이터를
         * 하나의 Gemini용 문자열로 만든다.
         */
        string behaviorSummary =
            BuildCurrentBehaviorSummary();


        /*
         * 현재 조사 결과를 세어서:
         *
         * 1. 실제 단서 몇 개를 찾았는지
         * 2. 탐사 기회를 몇 번 사용했는지
         *
         * 계산한다.
         */
        CountCurrentRoundResult(
            out int foundClues,
            out int usedAttempts
        );


        Debug.Log(
            "====================================\n" +
            $"=== ROUND {currentRound} END ===\n" +
            $"찾은 단서: {foundClues}/{TotalClueCount}\n" +
            $"사용한 탐사 기회: {usedAttempts}/{MaximumSearchAttempts}\n" +
            "===================================="
        );


        /*
         * ======================================
         * ROUND 1 종료
         * ======================================
         */
        if (currentRound == 1)
        {
            roundOneBehaviorSnapshot =
                behaviorSummary;


            roundOneFoundClues =
                foundClues;

            roundOneUsedAttempts =
                usedAttempts;


            Debug.Log(
                "=== ROUND 1 최종 플레이어 행동 데이터 ===\n" +
                roundOneBehaviorSnapshot
            );


            /*
             * ★ 기존 코드와 가장 중요한 차이 ★
             *
             * 여기서 바로 PrepareRoundTwo()를
             * 실행하지 않는다.
             *
             * 먼저 1라운드 결과 화면을 보여주고
             * 플레이어가 "다음 라운드" 버튼을
             * 누를 때까지 기다린다.
             */
            isWaitingForRoundTwoStart =
                true;


            ShowRoundOneResultScreen();
        }

        /*
         * ======================================
         * ROUND 2 종료
         * ======================================
         */
        else
        {
            roundTwoBehaviorSnapshot =
                behaviorSummary;


            roundTwoFoundClues =
                foundClues;

            roundTwoUsedAttempts =
                usedAttempts;


            Debug.Log(
                "=== ROUND 2 최종 플레이어 행동 데이터 ===\n" +
                roundTwoBehaviorSnapshot
            );


            /*
             * 2라운드는 마지막 라운드이므로
             * 최종 결과 화면으로 이동한다.
             */
            FinishGame();
        }
    }



    /*
     * ==========================================
     * 현재 라운드 결과 계산
     * ==========================================
     *
     * 조사 완료된 CCTVInteractable을 직접 센다.
     *
     * IsInvestigated == true:
     * 플레이어가 실제로 탐사 기회를 사용한 장소
     *
     * IsCrimeEvidence == true:
     * 실제 범죄 단서가 있었던 장소
     */
    private void CountCurrentRoundResult(
        out int foundClues,
        out int usedAttempts
    )
    {
        foundClues = 0;
        usedAttempts = 0;


        CCTVInteractable[] interactables =
            FindObjectsByType<CCTVInteractable>(
                FindObjectsSortMode.None
            );


        foreach (
            CCTVInteractable interactable
            in interactables
        )
        {
            if (
                interactable == null ||
                !interactable.IsInvestigated
            )
            {
                continue;
            }


            /*
             * 조사 완료된 장소 하나 =
             * 탐사 기회 하나 사용.
             */
            usedAttempts++;


            /*
             * 그 장소가 실제 범죄 단서였다면
             * 찾은 단서 +1.
             */
            if (interactable.IsCrimeEvidence)
            {
                foundClues++;
            }
        }


        /*
         * 혹시 다른 시스템 변경으로 값이 커져도
         * UI에는 게임 규칙 범위를 넘지 않도록 보호.
         */
        foundClues =
            Mathf.Clamp(
                foundClues,
                0,
                TotalClueCount
            );


        usedAttempts =
            Mathf.Clamp(
                usedAttempts,
                0,
                MaximumSearchAttempts
            );
    }



    /*
     * ==========================================
     * 1라운드 결과 화면
     * ==========================================
     */
    private void ShowRoundOneResultScreen()
    {
        /*
         * 혹시 최종 결과창이 켜져 있다면 제거.
         */
        if (finalResultPanel != null)
        {
            finalResultPanel.SetActive(
                false
            );
        }


        /*
         * 실제 결과 숫자 적용.
         */
        if (roundOneClueCountText != null)
        {
            roundOneClueCountText.text =
                "찾은 단서\n" +
                $"{roundOneFoundClues} / {TotalClueCount}";
        }


        if (roundOneAttemptsText != null)
        {
            roundOneAttemptsText.text =
                "사용한 탐사 기회\n" +
                $"{roundOneUsedAttempts} / {MaximumSearchAttempts}";
        }


        /*
         * 버튼 재사용 가능 상태.
         */
        if (nextRoundButton != null)
        {
            nextRoundButton.interactable =
                true;
        }


        /*
         * 결과 화면 표시.
         */
        if (roundOneResultPanel != null)
        {
            roundOneResultPanel.SetActive(
                true
            );
        }


        Debug.Log(
            "=== ROUND 1 결과 화면 표시 ===\n" +
            $"찾은 단서: {roundOneFoundClues}/{TotalClueCount}\n" +
            $"사용한 탐사 기회: {roundOneUsedAttempts}/{MaximumSearchAttempts}\n" +
            "다음 라운드 버튼 입력 대기"
        );
    }



    /*
     * ==========================================
     * 다음 라운드 버튼
     * ==========================================
     *
     * Next Round Button을 누르면 호출된다.
     *
     * 버튼의 On Click을 Inspector에서
     * 따로 연결할 필요 없이 Awake에서
     * 자동으로 연결된다.
     */
    public void ProceedToRoundTwo()
    {
        /*
         * 1라운드 결과 화면을 기다리는 상태가 아니면
         * 실행하지 않는다.
         *
         * 연속 클릭 방지 역할도 한다.
         */
        if (!isWaitingForRoundTwoStart)
        {
            return;
        }


        if (currentRound != 1)
        {
            return;
        }


        if (isWaitingForAI || isRoundPlaying)
        {
            return;
        }


        if (isGameFinished)
        {
            return;
        }


        /*
         * 버튼 중복 클릭 방지.
         */
        isWaitingForRoundTwoStart =
            false;


        if (nextRoundButton != null)
        {
            nextRoundButton.interactable =
                false;
        }


        /*
         * 1라운드 결과 화면을 닫는다.
         */
        HideRoundOneResultScreen();


        /*
         * 이제서야
         * 2라운드 Gemini 분석 단계로 이동.
         */
        StartCoroutine(
            PrepareRoundTwo()
        );
    }



    /*
     * ==========================================
     * 2라운드 준비
     * ==========================================
     *
     * 중요:
     *
     * 아직 PlayerActionRecorder 등의
     * 1라운드 행동 기록을 삭제하면 안 된다.
     *
     * Gemini가 이 데이터를 읽어서
     * 2라운드 배치를 변경해야 하기 때문이다.
     */
    private IEnumerator PrepareRoundTwo()
    {
        Debug.Log(
            "====================================\n" +
            "=== ROUND 2 준비 시작 ===\n" +
            "AI가 플레이어 행동을 분석하고 있습니다...\n" +
            "===================================="
        );


        /*
         * ShowAILoadingScreen보다 먼저
         * currentRound = 2.
         *
         * 그래야 AI 대기 문구가
         * 2라운드용으로 변경된다.
         */
        currentRound =
            2;


        isWaitingForAI =
            true;


        isRoundPlaying =
            false;


        isWaitingForRoundTwoStart =
            false;


        /*
         * AI 분석 중에는
         * 플레이어 조사 금지.
         */
        SetPlayerInteractionEnabled(
            false
        );


        /*
         * AI Loading Canvas 표시.
         *
         * 문구:
         * "AI가 플레이어의 행동을 분석하고 있습니다..."
         */
        ShowAILoadingScreen();


        /*
         * 기존 증거 배치,
         * HideZone 상태,
         * 조사 여부,
         * 탐사 횟수,
         * 증거 인벤토리 등을 초기화한다.
         *
         * 단,
         * Player 행동 데이터는 유지된다.
         */
        if (roundResetController != null)
        {
            roundResetController
                .ResetRoundState();
        }


        /*
         * 2라운드 타이머를
         * 다시 30초 대기 상태로 돌린다.
         */
        if (gameTimerHUD != null)
        {
            gameTimerHUD.ResetTimer();
        }


        Debug.Log(
            "=== ROUND 2 Gemini 요청 준비 완료 ===\n" +
            "1라운드 행동 데이터: 유지\n" +
            "증거 배치 상태: 초기화\n" +
            "탐사 기회: 초기화\n" +
            "타이머: 대기"
        );


        /*
         * Gemini는 여기서
         * 아직 남아 있는 1라운드 행동 데이터를 읽고
         * 2라운드 배치를 설계한다.
         */
        if (geminiHidePlanner != null)
        {
            yield return
                geminiHidePlanner
                    .RequestHidePlanForRound(
                        2
                    );
        }
        else
        {
            Debug.LogError(
                "GeminiHidePlanner가 없습니다."
            );
        }
    }



    /*
     * ==========================================
     * 2라운드 행동 기록 준비
     * ==========================================
     *
     * Gemini가 1라운드 행동 데이터를 사용한 뒤
     * 2라운드 배치까지 성공한 시점에 호출된다.
     */
    private void PrepareRecordersForRoundTwo()
    {
        Debug.Log(
            "=== 2라운드 행동 기록기 초기화 ==="
        );


        /*
         * 실제 조사 기록 초기화.
         */
        if (playerActionRecorder != null)
        {
            playerActionRecorder.ClearRecords();

            playerActionRecorder.SetCurrentRound(
                2
            );
        }


        /*
         * CCTV 체류 시간 초기화.
         */
        if (playerCameraRecorder != null)
        {
            playerCameraRecorder.ClearRecords();
        }


        /*
         * Hover 기록 초기화.
         */
        if (playerHoverRecorder != null)
        {
            playerHoverRecorder.ClearRecords();

            playerHoverRecorder.SetCurrentRound(
                2
            );
        }
    }



    /*
     * ==========================================
     * 행동 데이터 문자열 생성
     * ==========================================
     */
    private string BuildCurrentBehaviorSummary()
    {
        if (playerBehaviorSummary == null)
        {
            return
                "PlayerBehaviorSummary가 연결되어 있지 않습니다.";
        }


        return
            playerBehaviorSummary
                .BuildGeminiBehaviorSummary();
    }



    /*
     * ==========================================
     * 새로운 게임 시작 시
     * 행동 기록 전체 초기화
     * ==========================================
     */
    private void ClearAllBehaviorRecords()
    {
        if (playerActionRecorder != null)
        {
            playerActionRecorder.ClearRecords();
        }


        if (playerCameraRecorder != null)
        {
            playerCameraRecorder.ClearRecords();
        }


        if (playerHoverRecorder != null)
        {
            playerHoverRecorder.ClearRecords();
        }
    }



    /*
     * ==========================================
     * CCTV 조사 기능 ON / OFF
     * ==========================================
     */
    private void SetPlayerInteractionEnabled(
        bool enabledState
    )
    {
        if (cctvCursorInteractor == null)
        {
            return;
        }


        cctvCursorInteractor.enabled =
            enabledState;
    }



    /*
     * ==========================================
     * AI 대기 화면 표시
     * ==========================================
     */
    private void ShowAILoadingScreen()
    {
        if (aiLoadingPanel == null)
        {
            return;
        }


        /*
         * 현재 라운드에 따라
         * AI 문구를 다르게 표시한다.
         */
        if (aiLoadingText != null)
        {
            if (currentRound == 1)
            {
                aiLoadingText.text =
                    "AI가 단서를 숨기고 있습니다...";
            }
            else
            {
                aiLoadingText.text =
                    "AI가 플레이어의 행동을 분석하고 있습니다...";
            }
        }


        aiLoadingPanel.SetActive(
            true
        );
    }



    /*
     * ==========================================
     * AI 대기 화면 종료
     * ==========================================
     */
    private void HideAILoadingScreen()
    {
        if (aiLoadingPanel == null)
        {
            return;
        }


        aiLoadingPanel.SetActive(
            false
        );
    }



    /*
     * ==========================================
     * 1라운드 결과 화면 종료
     * ==========================================
     */
    private void HideRoundOneResultScreen()
    {
        if (roundOneResultPanel == null)
        {
            return;
        }


        roundOneResultPanel.SetActive(
            false
        );
    }



    /*
     * ==========================================
     * 모든 결과 UI 끄기
     * ==========================================
     */
    private void HideAllResultScreens()
    {
        if (roundOneResultPanel != null)
        {
            roundOneResultPanel.SetActive(
                false
            );
        }


        if (finalResultPanel != null)
        {
            finalResultPanel.SetActive(
                false
            );
        }
    }



    /*
     * ==========================================
     * 최종 결과 화면 표시
     * ==========================================
     */
    private void ShowFinalResultScreen()
    {
        /*
         * 혹시 1라운드 결과 화면이 남아 있다면 제거.
         */
        if (roundOneResultPanel != null)
        {
            roundOneResultPanel.SetActive(
                false
            );
        }


        /*
         * 2라운드 실제 결과 표시.
         */
        if (finalClueCountText != null)
        {
            finalClueCountText.text =
                "찾은 단서\n" +
                $"{roundTwoFoundClues} / {TotalClueCount}";
        }


        if (finalAttemptsText != null)
        {
            finalAttemptsText.text =
                "사용한 탐사 기회\n" +
                $"{roundTwoUsedAttempts} / {MaximumSearchAttempts}";
        }


        /*
         * Gemini가 2라운드를 설계하면서 만든
         * strategySummary를 최종 분석에 표시한다.
         *
         * 여기서는 추가 Gemini API 요청을 하지 않는다.
         *
         * 즉,
         * 최종 결과 화면을 띄운다고
         * 토큰/요청이 추가로 소비되지 않는다.
         */
        if (finalStrategySummaryText != null)
        {
            string strategySummary =
                string.Empty;


            if (geminiHidePlanner != null)
            {
                strategySummary =
                    geminiHidePlanner
                        .LastStrategySummary;
            }


            if (string.IsNullOrWhiteSpace(strategySummary))
            {
                strategySummary =
                    "AI 전략 분석 결과를 불러오지 못했습니다.";
            }


            finalStrategySummaryText.text =
                strategySummary;
        }


        /*
         * 최종 결과 화면 표시.
         */
        if (finalResultPanel != null)
        {
            finalResultPanel.SetActive(
                true
            );
        }
    }



    /*
     * ==========================================
     * 전체 게임 종료
     * ==========================================
     */
    private void FinishGame()
    {
        isWaitingForAI =
            false;

        isRoundPlaying =
            false;

        isWaitingForRoundTwoStart =
            false;

        isGameFinished =
            true;


        /*
         * 게임 종료 후 조사 금지.
         */
        SetPlayerInteractionEnabled(
            false
        );


        /*
         * 혹시 타이머가 남아 있다면 정지.
         */
        if (gameTimerHUD != null)
        {
            gameTimerHUD.StopTimer();
        }


        Debug.Log(
            "====================================\n" +
            "=== SceneCleaner GAME END ===\n" +
            "===================================="
        );


        /*
         * 기존처럼 Console에도
         * Gemini 전략 분석을 남긴다.
         */
        if (geminiHidePlanner != null)
        {
            Debug.Log(
                "=== Gemini 최종 전략 분석 ===\n" +
                geminiHidePlanner.LastStrategySummary
            );
        }


        /*
         * 이제 Console뿐 아니라
         * 실제 최종 결과 UI를 표시한다.
         */
        ShowFinalResultScreen();
    }



    /*
     * ==========================================
     * 테스트용
     * ==========================================
     */
    [ContextMenu("게임 시작")]
    private void StartGameForTest()
    {
        StartGame();
    }
}