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

    [Header("Gemini 설정")]

    [SerializeField]
    private string modelName = "gemini-3.6-flash";

    [SerializeField]
    [Range(0f, 1f)]
    private float temperature = 0.2f;

    [SerializeField]
    private int requestTimeoutSeconds = 30;

    private string apiKey;
    private bool isRequesting;

    [ContextMenu("Gemini 숨김 계획 요청 및 적용 테스트")]
    private void RequestHidePlanForTest()
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

        StartCoroutine(RequestHidePlan());
    }

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

        string prompt =
            promptBuilder.BuildPrompt();

        if (string.IsNullOrWhiteSpace(prompt))
        {
            Debug.LogError(
                "Gemini에 전달할 숨김 프롬프트를 " +
                "만들지 못했습니다."
            );

            yield break;
        }

        isRequesting = true;

        string requestJson =
            BuildRequestJson(prompt);

        Debug.Log(
            "Gemini 숨김 계획 요청 시작"
        );

        yield return SendRequest(requestJson);

        isRequesting = false;
    }

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

        return JsonUtility.ToJson(requestBody);
    }

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
                        }
                },

            required =
                new string[]
                {
                    "assignments"
                }
        };
    }

    private IEnumerator SendRequest(
        string requestJson
    )
    {
        string url =
            "https://generativelanguage.googleapis.com/" +
            $"v1beta/models/{modelName}:generateContent";

        byte[] requestBody =
            Encoding.UTF8.GetBytes(requestJson);

        using (
            UnityWebRequest request =
                new UnityWebRequest(
                    url,
                    UnityWebRequest.kHttpVerbPOST
                )
        )
        {
            request.uploadHandler =
                new UploadHandlerRaw(requestBody);

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
                ExtractResponseText(responseJson);

            if (string.IsNullOrWhiteSpace(planJson))
            {
                Debug.LogError(
                    "Gemini 응답에서 숨김 계획 JSON을 " +
                    "찾지 못했습니다.\n" +
                    $"전체 응답:\n{responseJson}"
                );

                yield break;
            }

            ParseAndApplyPlan(planJson);
        }
    }

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
                textBuilder.Append(part.text);
            }
        }

        return textBuilder.ToString().Trim();
    }

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
            "[Gemini 반환 JSON]"
        );

        resultBuilder.AppendLine(planJson);

        Debug.Log(
            resultBuilder.ToString().Trim()
        );

        bool applySucceeded =
            hideController.ApplyHidePlan(plan);

        if (!applySucceeded)
        {
            Debug.LogError(
                "Gemini의 숨김 계획은 생성됐지만 " +
                "Unity 장면 적용에 실패했습니다."
            );

            return;
        }

        Debug.Log(
            "Gemini가 선택한 위치로 " +
            "모든 물건을 이동했습니다."
        );
    }
}

/*
 * Gemini 요청 데이터 구조
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
 * Gemini API 응답 데이터 구조
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
 * 실제 숨김 계획 데이터
 */

[Serializable]
public class AIHidePlan
{
    public AIHideAssignment[] assignments;
}

[Serializable]
public class AIHideAssignment
{
    public string evidenceId;
    public string hideZoneId;
}