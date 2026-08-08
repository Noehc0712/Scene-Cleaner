using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GeminiHidePlanner : MonoBehaviour
{
    [Header("필수 연결")]

    [SerializeField]
    private AIHidePromptBuilder promptBuilder;

    [SerializeField]
    private AIHideController hideController;


    [Header("플레이어 행동 분석")]

    [SerializeField]
    private PlayerBehaviorSummary playerBehaviorSummary;


    [Header("현재 라운드")]

    [SerializeField, Min(1)]
    private int currentRound = 1;


    [Header("Gemini 설정")]

    [SerializeField]
    private string modelName = "gemini-3.6-flash";

    [SerializeField]
    [Range(0f, 1f)]
    private float temperature = 0.2f;

    [SerializeField]
    private int requestTimeoutSeconds = 30;


    [Header("최근 Gemini 결과")]

    [SerializeField, TextArea(3, 8)]
    private string lastStrategySummary;


    private string apiKey;
    private bool isRequesting;


    public int CurrentRound
    {
        get
        {
            return currentRound;
        }
    }


    public bool IsRequesting
    {
        get
        {
            return isRequesting;
        }
    }


    /// <summary>
    /// 가장 최근 Gemini가 반환한
    /// 숨김 전략 설명이다.
    ///
    /// 이후 최종 결과 UI에서 사용할 수 있다.
    /// </summary>
    public string LastStrategySummary
    {
        get
        {
            return lastStrategySummary;
        }
    }


    private void Awake()
    {
        /*
         * PlayerBehaviorSummary가 Inspector에서
         * 연결되지 않았다면 씬에서 자동으로 찾는다.
         */
        if (playerBehaviorSummary == null)
        {
            playerBehaviorSummary =
                FindFirstObjectByType<PlayerBehaviorSummary>();
        }
    }


    // =============================================================
    // 테스트용 Context Menu
    // =============================================================

    /// <summary>
    /// Inspector의 Current Round 값을 기준으로
    /// Gemini 숨김 계획을 요청한다.
    /// </summary>
    [ContextMenu("Gemini 숨김 계획 요청 및 적용 테스트")]
    private void RequestHidePlanForTest()
    {
        StartHidePlanRequest();
    }


    /// <summary>
    /// 개발 중 1라운드를 바로 테스트하기 위한 메뉴.
    /// </summary>
    [ContextMenu("1라운드 Gemini 숨김 테스트")]
    private void RequestRoundOneForTest()
    {
        SetCurrentRound(1);

        StartHidePlanRequest();
    }


    /// <summary>
    /// 개발 중 2라운드 적응형 숨김을
    /// 바로 테스트하기 위한 메뉴.
    ///
    /// 이 메뉴를 실행하기 전에는
    /// 기존 EvidenceObject / HideZone 상태를
    /// 초기화해야 한다.
    /// </summary>
    [ContextMenu("2라운드 적응형 Gemini 숨김 테스트")]
    private void RequestRoundTwoForTest()
    {
        SetCurrentRound(2);

        StartHidePlanRequest();
    }


    /// <summary>
    /// 실제 Coroutine 요청 시작 전
    /// 공통 검사를 수행한다.
    /// </summary>
    private void StartHidePlanRequest()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning(
                "Gemini 요청은 Play 모드에서 실행해 주세요."
            );

            return;
        }


        if (isRequesting)
        {
            Debug.LogWarning(
                "이미 Gemini 요청을 처리하고 있습니다."
            );

            return;
        }


        StartCoroutine(
            RequestHidePlan()
        );
    }


    // =============================================================
    // 라운드 관리
    // =============================================================

    /// <summary>
    /// 현재 Gemini가 계획할 라운드를 설정한다.
    ///
    /// 1라운드:
    /// 플레이어 행동 데이터를 사용하지 않는다.
    ///
    /// 2라운드:
    /// 1라운드에서 수집한 플레이어 행동 데이터를
    /// Gemini 프롬프트에 포함한다.
    /// </summary>
    public void SetCurrentRound(int roundNumber)
    {
        if (roundNumber < 1)
        {
            Debug.LogError(
                "Gemini Hide Planner의 라운드 번호는 " +
                "1 이상이어야 합니다."
            );

            return;
        }


        currentRound = roundNumber;


        Debug.Log(
            $"Gemini Hide Planner를 " +
            $"{currentRound}라운드로 설정했습니다."
        );
    }


    /// <summary>
    /// 이후 GameFlowManager 등에서
    /// 원하는 라운드를 지정하여 호출할 수 있는 함수.
    /// </summary>
    public IEnumerator RequestHidePlanForRound(
        int roundNumber
    )
    {
        SetCurrentRound(roundNumber);

        yield return RequestHidePlan();
    }


    // =============================================================
    // Gemini 요청
    // =============================================================

    /// <summary>
    /// 프롬프트 생성부터 Gemini 요청,
    /// 결과 적용까지 진행한다.
    /// </summary>
    public IEnumerator RequestHidePlan()
    {
        if (!LoadApiKey())
        {
            yield break;
        }


        if (promptBuilder == null)
        {
            Debug.LogError(
                "AI Hide Prompt Builder가 연결되지 않았습니다."
            );

            yield break;
        }


        if (hideController == null)
        {
            Debug.LogError(
                "AI Hide Controller가 연결되지 않았습니다."
            );

            yield break;
        }


        /*
         * 기존 AIHidePromptBuilder가 만드는
         * 증거물 / HideZone 기본 프롬프트를 가져온다.
         */
        string basePrompt =
            promptBuilder.BuildPrompt();


        if (string.IsNullOrWhiteSpace(basePrompt))
        {
            Debug.LogError(
                "Gemini에 전달할 숨김 프롬프트를 " +
                "만들지 못했습니다."
            );

            yield break;
        }


        /*
         * 현재 라운드에 맞는 추가 지시사항을 붙인다.
         */
        string prompt =
            BuildRoundPrompt(basePrompt);


        if (string.IsNullOrWhiteSpace(prompt))
        {
            Debug.LogError(
                "라운드별 Gemini 프롬프트를 " +
                "생성하지 못했습니다."
            );

            yield break;
        }


        isRequesting = true;


        string requestJson =
            BuildRequestJson(prompt);


        Debug.Log(
            "=== Gemini 숨김 계획 요청 시작 ===\n" +
            $"현재 라운드: {currentRound}"
        );


        /*
         * 개발 중에는 Gemini가 실제로 어떤 정보를
         * 받았는지 확인하기 위해 프롬프트를 출력한다.
         *
         * API Key는 프롬프트 안에 포함되지 않는다.
         */
        Debug.Log(
            "=== Gemini 전달 프롬프트 ===\n" +
            prompt
        );


        yield return SendRequest(
            requestJson
        );


        isRequesting = false;
    }


    // =============================================================
    // 라운드별 프롬프트
    // =============================================================

    /// <summary>
    /// 기존 AIHidePromptBuilder의 프롬프트에
    /// 현재 라운드에 필요한 전략 지시를 추가한다.
    /// </summary>
    private string BuildRoundPrompt(
        string basePrompt
    )
    {
        StringBuilder prompt =
            new StringBuilder();


        prompt.AppendLine(basePrompt);

        prompt.AppendLine();

        prompt.AppendLine(
            "========================================"
        );


        // =========================================================
        // ROUND 1
        // =========================================================

        if (currentRound == 1)
        {
            prompt.AppendLine(
                "[현재 게임 단계: ROUND 1]"
            );

            prompt.AppendLine();

            prompt.AppendLine(
                "이번 라운드는 플레이어 행동 데이터가 없는 " +
                "첫 번째 라운드이다."
            );

            prompt.AppendLine();

            prompt.AppendLine(
                "특정 장소에 지나치게 편향되지 않도록 " +
                "전체 공간과 장소 특성을 고려하여 " +
                "자연스럽게 증거물과 미끼를 배치하라."
            );

            prompt.AppendLine();

            prompt.AppendLine(
                "strategySummary에는 이번 1라운드에서 " +
                "어떤 기준으로 장소를 선택했는지 " +
                "한국어로 간단히 설명하라."
            );


            return prompt.ToString();
        }


        // =========================================================
        // ROUND 2 이상
        // =========================================================

        prompt.AppendLine(
            $"[현재 게임 단계: ROUND {currentRound}]"
        );

        prompt.AppendLine();

        prompt.AppendLine(
            "이번 라운드는 이전 라운드에서 수집한 " +
            "플레이어 행동을 분석하여 숨김 전략을 " +
            "조정해야 하는 적응형 라운드이다."
        );

        prompt.AppendLine();


        /*
         * PlayerBehaviorSummary 연결 확인
         */
        if (playerBehaviorSummary == null)
        {
            playerBehaviorSummary =
                FindFirstObjectByType<PlayerBehaviorSummary>();
        }


        if (playerBehaviorSummary == null)
        {
            Debug.LogError(
                "2라운드 적응형 숨김에 필요한 " +
                "PlayerBehaviorSummary를 찾을 수 없습니다."
            );

            return null;
        }


        string behaviorSummary =
            playerBehaviorSummary
                .BuildGeminiBehaviorSummary();


        prompt.AppendLine(
            "===== 이전 라운드 플레이어 행동 데이터 ====="
        );

        prompt.AppendLine();

        prompt.AppendLine(
            behaviorSummary
        );

        prompt.AppendLine(
            "===== 행동 데이터 끝 ====="
        );

        prompt.AppendLine();


        // =========================================================
        // Gemini가 행동 데이터를 해석하는 기준
        // =========================================================

        prompt.AppendLine(
            "[적응형 숨김 전략 지침]"
        );

        prompt.AppendLine();


        prompt.AppendLine(
            "1. CCTV 1과 CCTV 2는 방을 보여주며, " +
            "CCTV 3과 CCTV 4는 주방을 보여준다."
        );


        prompt.AppendLine(
            "2. 방과 주방의 체류 시간을 비교하여 " +
            "플레이어가 어느 공간을 더 집중적으로 " +
            "관찰했는지 판단하라."
        );


        prompt.AppendLine(
            "3. 마우스 진입 횟수와 총 관심 시간을 함께 보고 " +
            "플레이어가 반복적으로 의심한 장소와 " +
            "거의 관심을 보이지 않은 장소를 판단하라."
        );


        prompt.AppendLine(
            "4. 실제 조사 순서와 조사 결과 " +
            "(Clue, Decoy, Empty)도 함께 고려하라."
        );


        prompt.AppendLine(
            "5. 플레이어가 집중적으로 관찰하거나 " +
            "반복해서 확인한 장소에는 실제 단서를 줄이고, " +
            "미끼를 배치하거나 아무것도 배치하지 않는 " +
            "전략을 사용할 수 있다."
        );


        prompt.AppendLine(
            "6. 플레이어가 거의 보지 않았거나 " +
            "관심이 낮았던 장소에는 실제 단서를 " +
            "더 적극적으로 배치할 수 있다."
        );


        prompt.AppendLine(
            "7. 특정 공간을 플레이어가 매우 오래 관찰했다면 " +
            "그 공간의 실제 단서 수를 줄이고, " +
            "상대적으로 덜 관찰한 공간에 실제 단서를 " +
            "더 배치하는 전략을 고려하라."
        );


        prompt.AppendLine(
            "8. 플레이어가 이전 라운드에서 " +
            "미끼에 속았던 장소나 비슷한 패턴도 " +
            "새로운 교란 전략에 활용할 수 있다."
        );


        prompt.AppendLine(
            "9. 하지만 위 규칙을 단순한 고정 공식처럼 " +
            "적용하지 말고, 체류 시간, Hover 관심도, " +
            "실제 조사 행동을 종합하여 판단하라."
        );


        prompt.AppendLine(
            "10. 플레이어 행동의 정반대로만 배치하지 말라. " +
            "일부 예측 가능성을 남기면서도 전체적으로 " +
            "1라운드보다 적응된 숨김 전략을 만들어라."
        );


        prompt.AppendLine();

        prompt.AppendLine(
            "[매우 중요한 기존 배치 규칙]"
        );


        prompt.AppendLine(
            "- 모든 evidenceId는 반드시 정확히 한 번씩 사용해야 한다."
        );


        prompt.AppendLine(
            "- 같은 hideZoneId를 두 증거물이 동시에 사용할 수 없다."
        );


        prompt.AppendLine(
            "- PhysicalObject와 SurfaceTrace의 " +
            "장소 호환 조건을 반드시 지켜야 한다."
        );


        prompt.AppendLine(
            "- 제공되지 않은 evidenceId 또는 hideZoneId를 " +
            "새로 만들어서는 안 된다."
        );


        prompt.AppendLine(
            "- 실제 단서와 미끼를 포함한 모든 EvidenceObject를 " +
            "반드시 배치해야 한다."
        );


        prompt.AppendLine();

        prompt.AppendLine(
            "[strategySummary 작성 규칙]"
        );


        prompt.AppendLine(
            "strategySummary에는 플레이어의 실제 행동 데이터를 " +
            "근거로 이번 라운드에서 어떤 숨김 전략을 사용했는지 " +
            "한국어로 2~4문장 정도 설명하라."
        );


        prompt.AppendLine(
            "가능하면 다음과 같은 구체적인 근거를 포함하라: " +
            "어느 공간을 오래 관찰했는지, " +
            "어느 장소를 반복해서 살폈는지, " +
            "그 결과 실제 단서 또는 미끼를 " +
            "어떻게 재배치했는지."
        );


        return prompt.ToString();
    }


    // =============================================================
    // API Key
    // =============================================================

    private bool LoadApiKey()
    {
        apiKey =
            Environment.GetEnvironmentVariable(
                "GEMINI_API_KEY",
                EnvironmentVariableTarget.User
            );


        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Debug.LogError(
                "GEMINI_API_KEY 환경변수를 찾지 못했습니다."
            );

            return false;
        }


        Debug.Log(
            "Gemini API 키 확인 성공"
        );


        return true;
    }


    // =============================================================
    // 요청 JSON 생성
    // =============================================================

    private string BuildRequestJson(
        string prompt
    )
    {
        GeminiHideGenerateContentRequest requestBody =
            new GeminiHideGenerateContentRequest
            {
                contents =
                    new GeminiHideRequestContent[]
                    {
                        new GeminiHideRequestContent
                        {
                            parts =
                                new GeminiHideRequestPart[]
                                {
                                    new GeminiHideRequestPart
                                    {
                                        text = prompt
                                    }
                                }
                        }
                    },


                generationConfig =
                    new GeminiHideGenerationConfig
                    {
                        responseMimeType =
                            "application/json",

                        temperature =
                            temperature,

                        responseSchema =
                            CreateResponseSchema()
                    }
            };


        return JsonUtility.ToJson(
            requestBody
        );
    }


    // =============================================================
    // Gemini Structured Output Schema
    // =============================================================

    private GeminiHideResponseSchema
        CreateResponseSchema()
    {
        return new GeminiHideResponseSchema
        {
            type = "OBJECT",


            properties =
                new GeminiHideRootProperties
                {
                    assignments =
                        new GeminiHideArraySchema
                        {
                            type = "ARRAY",


                            items =
                                new GeminiHideAssignmentSchema
                                {
                                    type = "OBJECT",


                                    properties =
                                        new GeminiHideAssignmentProperties
                                        {
                                            evidenceId =
                                                new GeminiHideStringSchema
                                                {
                                                    type = "STRING",

                                                    description =
                                                        "제공된 물건의 evidenceId"
                                                },


                                            hideZoneId =
                                                new GeminiHideStringSchema
                                                {
                                                    type = "STRING",

                                                    description =
                                                        "제공된 장소의 hideZoneId"
                                                }
                                        },


                                    required =
                                        new string[]
                                        {
                                            "evidenceId",
                                            "hideZoneId"
                                        }
                                }
                        },


                    /*
                     * Gemini가 자신의 배치 전략을
                     * 한국어 문장으로 설명한다.
                     *
                     * 이후 최종 결과 화면에서 사용 가능하다.
                     */
                    strategySummary =
                        new GeminiHideStringSchema
                        {
                            type = "STRING",

                            description =
                                "이번 라운드에서 사용한 숨김 전략을 " +
                                "한국어로 설명한 요약. " +
                                "2라운드에서는 플레이어 행동 데이터와 " +
                                "배치 변경의 관계를 구체적으로 설명한다."
                        }
                },


            required =
                new string[]
                {
                    "assignments",
                    "strategySummary"
                }
        };
    }


    // =============================================================
    // Gemini HTTP 요청
    // =============================================================

    private IEnumerator SendRequest(
        string requestJson
    )
    {
        string url =
            "https://generativelanguage.googleapis.com/" +
            $"v1beta/models/{modelName}:generateContent";


        byte[] requestBody =
            Encoding.UTF8.GetBytes(
                requestJson
            );


        using (
            UnityWebRequest request =
                new UnityWebRequest(
                    url,
                    UnityWebRequest.kHttpVerbPOST
                )
        )
        {
            request.uploadHandler =
                new UploadHandlerRaw(
                    requestBody
                );


            request.downloadHandler =
                new DownloadHandlerBuffer();


            request.timeout =
                requestTimeoutSeconds;


            request.SetRequestHeader(
                "Content-Type",
                "application/json"
            );


            request.SetRequestHeader(
                "x-goog-api-key",
                apiKey
            );


            yield return request.SendWebRequest();


            if (request.result !=
                UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    "Gemini 숨김 계획 요청 실패\n" +
                    $"Result: {request.result}\n" +
                    $"Code: {request.responseCode}\n" +
                    $"Message: {request.error}\n" +
                    $"Body: {request.downloadHandler.text}"
                );


                yield break;
            }


            string responseJson =
                request.downloadHandler.text;


            string planJson =
                ExtractResponseText(
                    responseJson
                );


            if (string.IsNullOrWhiteSpace(planJson))
            {
                Debug.LogError(
                    "Gemini 응답에서 숨김 계획 JSON을 " +
                    "찾지 못했습니다.\n" +
                    $"전체 응답:\n{responseJson}"
                );


                yield break;
            }


            ParseAndApplyPlan(
                planJson
            );
        }
    }


    // =============================================================
    // Gemini API 응답 Text 추출
    // =============================================================

    private string ExtractResponseText(
        string responseJson
    )
    {
        GeminiHideApiResponse response =
            JsonUtility.FromJson
                <GeminiHideApiResponse>(
                    responseJson
                );


        if (response == null ||
            response.candidates == null ||
            response.candidates.Length == 0)
        {
            return null;
        }


        GeminiHideCandidate firstCandidate =
            response.candidates[0];


        if (firstCandidate.content == null ||
            firstCandidate.content.parts == null ||
            firstCandidate.content.parts.Length == 0)
        {
            return null;
        }


        StringBuilder textBuilder =
            new StringBuilder();


        foreach (
            GeminiHideApiPart part
            in firstCandidate.content.parts
        )
        {
            if (!string.IsNullOrWhiteSpace(
                    part.text
                ))
            {
                textBuilder.Append(
                    part.text
                );
            }
        }


        return textBuilder
            .ToString()
            .Trim();
    }


    // =============================================================
    // Gemini 계획 파싱 및 적용
    // =============================================================

    /// <summary>
    /// Gemini JSON을 AIHidePlan으로 변환하고
    /// AIHideController에 전달한다.
    /// </summary>
    private void ParseAndApplyPlan(
        string planJson
    )
    {
        AIHidePlan plan;


        try
        {
            plan =
                JsonUtility.FromJson<AIHidePlan>(
                    planJson
                );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Gemini 숨김 계획 JSON 파싱 실패\n" +
                exception.Message +
                "\n\n받은 JSON:\n" +
                planJson
            );


            return;
        }


        if (plan == null ||
            plan.assignments == null ||
            plan.assignments.Length == 0)
        {
            Debug.LogError(
                "Gemini가 숨김 배치 정보를 " +
                "반환하지 않았습니다.\n" +
                $"받은 JSON:\n{planJson}"
            );


            return;
        }


        StringBuilder resultBuilder =
            new StringBuilder();


        resultBuilder.AppendLine(
            "=== Gemini 숨김 계획 생성 성공 ==="
        );


        resultBuilder.AppendLine(
            $"라운드: {currentRound}"
        );


        resultBuilder.AppendLine();


        foreach (
            AIHideAssignment assignment
            in plan.assignments
        )
        {
            resultBuilder.AppendLine(
                $"물건 ID: {assignment.evidenceId}"
            );


            resultBuilder.AppendLine(
                $"선택 장소 ID: {assignment.hideZoneId}"
            );


            resultBuilder.AppendLine();
        }


        resultBuilder.AppendLine(
            "[Gemini 숨김 전략]"
        );


        resultBuilder.AppendLine(
            string.IsNullOrWhiteSpace(
                plan.strategySummary
            )
                ? "전략 설명 없음"
                : plan.strategySummary
        );


        resultBuilder.AppendLine();


        resultBuilder.AppendLine(
            "[Gemini 반환 JSON]"
        );


        resultBuilder.AppendLine(
            planJson
        );


        Debug.Log(
            resultBuilder
                .ToString()
                .Trim()
        );


        /*
         * 기존 AIHideController에서
         * ID / 중복 / PlacementType / HideZone 상태를
         * 다시 검증한 뒤 실제 배치를 적용한다.
         */
        bool applySucceeded =
            hideController.ApplyHidePlan(
                plan
            );


        if (!applySucceeded)
        {
            Debug.LogError(
                "Gemini의 숨김 계획은 생성됐지만 " +
                "Unity 장면 적용에 실패했습니다."
            );


            return;
        }


        /*
         * 배치까지 정상 완료된 경우에만
         * 최종 전략 설명으로 저장한다.
         */
        lastStrategySummary =
            plan.strategySummary;


        Debug.Log(
            "=== Gemini 숨김 계획 적용 완료 ===\n" +
            $"라운드: {currentRound}\n" +
            $"전략: {lastStrategySummary}"
        );


        Debug.Log(
            "Gemini가 선택한 위치로 " +
            "모든 물건을 이동했습니다."
        );
    }
}


/*
 * =============================================================
 * Gemini 요청 데이터 구조
 * =============================================================
 */

[Serializable]
public class GeminiHideGenerateContentRequest
{
    public GeminiHideRequestContent[] contents;
    public GeminiHideGenerationConfig generationConfig;
}


[Serializable]
public class GeminiHideRequestContent
{
    public GeminiHideRequestPart[] parts;
}


[Serializable]
public class GeminiHideRequestPart
{
    public string text;
}


[Serializable]
public class GeminiHideGenerationConfig
{
    public string responseMimeType;
    public float temperature;
    public GeminiHideResponseSchema responseSchema;
}


[Serializable]
public class GeminiHideResponseSchema
{
    public string type;
    public GeminiHideRootProperties properties;
    public string[] required;
}


[Serializable]
public class GeminiHideRootProperties
{
    public GeminiHideArraySchema assignments;

    public GeminiHideStringSchema strategySummary;
}


[Serializable]
public class GeminiHideArraySchema
{
    public string type;
    public GeminiHideAssignmentSchema items;
}


[Serializable]
public class GeminiHideAssignmentSchema
{
    public string type;
    public GeminiHideAssignmentProperties properties;
    public string[] required;
}


[Serializable]
public class GeminiHideAssignmentProperties
{
    public GeminiHideStringSchema evidenceId;
    public GeminiHideStringSchema hideZoneId;
}


[Serializable]
public class GeminiHideStringSchema
{
    public string type;
    public string description;
}


/*
 * =============================================================
 * Gemini API 응답 데이터 구조
 * =============================================================
 */

[Serializable]
public class GeminiHideApiResponse
{
    public GeminiHideCandidate[] candidates;
}


[Serializable]
public class GeminiHideCandidate
{
    public GeminiHideApiContent content;
}


[Serializable]
public class GeminiHideApiContent
{
    public GeminiHideApiPart[] parts;
}


[Serializable]
public class GeminiHideApiPart
{
    public string text;
}


/*
 * =============================================================
 * 실제 숨김 계획 데이터
 * =============================================================
 */

[Serializable]
public class AIHidePlan
{
    public AIHideAssignment[] assignments;

    /*
     * Gemini가 이번 배치 전략을
     * 설명하는 한국어 문장.
     */
    public string strategySummary;
}


[Serializable]
public class AIHideAssignment
{
    public string evidenceId;
    public string hideZoneId;
}