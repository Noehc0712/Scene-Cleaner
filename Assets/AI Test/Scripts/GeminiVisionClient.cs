using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GeminiVisionClient : MonoBehaviour
{
    [Header("Gemini 설정")]

    [SerializeField]
    private string modelName = "gemini-3.6-flash";

    [SerializeField]
    [TextArea(3, 6)]
    private string analysisPrompt =
        "이 이미지를 보고 장면을 간단히 분석해줘. " +
        "보이는 주요 물체를 나열하고, " +
        "장면이 자연스러운지 한 줄로 평가해줘.";

    [Header("Unity 증거물 상태")]

    [SerializeField]
    private EvidenceStateCollector evidenceStateCollector;

    private string apiKey;

    private void Start()
    {
        LoadApiKey();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return;
        }

        StartCoroutine(RunVisionTest());
    }

    /// <summary>
    /// Windows 사용자 환경변수에서 Gemini API 키를 읽는다.
    /// 실제 API 키 문자열은 Console에 출력하지 않는다.
    /// </summary>
    private void LoadApiKey()
    {
        apiKey = Environment.GetEnvironmentVariable(
            "GEMINI_API_KEY",
            EnvironmentVariableTarget.User
        );

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Debug.LogError(
                "GEMINI_API_KEY 환경변수를 찾지 못했습니다."
            );

            return;
        }

        Debug.Log("Gemini API 키 확인 성공");
    }

    /// <summary>
    /// 최근 캡처 이미지를 찾아 Gemini 분석을 실행한다.
    /// </summary>
    private IEnumerator RunVisionTest()
    {
        // SceneCapture가 PNG 파일을 저장할 시간을 준다.
        yield return new WaitForSeconds(0.5f);

        string imagePath = FindLatestCapturePath();

        if (string.IsNullOrWhiteSpace(imagePath))
        {
            Debug.LogError(
                "전송할 캡처 이미지를 찾지 못했습니다."
            );

            yield break;
        }

        Debug.Log($"전송할 캡처 이미지: {imagePath}");

        byte[] imageBytes;

        try
        {
            imageBytes = File.ReadAllBytes(imagePath);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "캡처 이미지 읽기 실패\n" +
                exception.Message
            );

            yield break;
        }

        string base64Image =
            Convert.ToBase64String(imageBytes);

        string requestJson =
            BuildRequestJson(base64Image);

        yield return SendVisionRequest(requestJson);
    }

    /// <summary>
    /// 프로젝트의 Captures 폴더에서
    /// 가장 최근에 저장된 PNG 파일을 찾는다.
    /// </summary>
    private string FindLatestCapturePath()
    {
        DirectoryInfo projectDirectory =
            Directory.GetParent(Application.dataPath);

        if (projectDirectory == null)
        {
            Debug.LogError(
                "Unity 프로젝트 경로를 찾지 못했습니다."
            );

            return null;
        }

        string captureFolder = Path.Combine(
            projectDirectory.FullName,
            "Captures"
        );

        if (!Directory.Exists(captureFolder))
        {
            Debug.LogError(
                $"Captures 폴더가 없습니다: {captureFolder}"
            );

            return null;
        }

        FileInfo latestFile = new DirectoryInfo(captureFolder)
            .GetFiles("*.png")
            .OrderByDescending(file => file.LastWriteTime)
            .FirstOrDefault();

        if (latestFile == null)
        {
            Debug.LogError(
                "Captures 폴더 안에 PNG 파일이 없습니다."
            );

            return null;
        }

        return latestFile.FullName;
    }

    /// <summary>
    /// Gemini에 전송할 JSON 요청 본문을 만든다.
    ///
    /// 첫 번째 Part에는 프롬프트만,
    /// 두 번째 Part에는 이미지 데이터만 넣는다.
    /// </summary>
    private string BuildRequestJson(string base64Image)
    {
        GeminiTextPartPayload textPart =
            new GeminiTextPartPayload
            {
                text = analysisPrompt
            };

        string textPartJson =
            JsonUtility.ToJson(textPart);

        string imagePartJson =
            "{\"inline_data\":{" +
            "\"mime_type\":\"image/png\"," +
            "\"data\":\"" + base64Image + "\"" +
            "}}";

        string requestJson =
            "{\"contents\":[{" +
            "\"parts\":[" +
            textPartJson + "," +
            imagePartJson +
            "]" +
            "}]}";

        return requestJson;
    }

    /// <summary>
    /// Gemini API에 이미지 분석 요청을 보낸다.
    /// </summary>
    private IEnumerator SendVisionRequest(
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

            request.SetRequestHeader(
                "Content-Type",
                "application/json"
            );

            request.SetRequestHeader(
                "x-goog-api-key",
                apiKey
            );

            Debug.Log("Gemini 이미지 분석 요청 시작");

            yield return request.SendWebRequest();

            if (request.result !=
                UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    "Gemini 요청 실패\n" +
                    $"Result: {request.result}\n" +
                    $"Code: {request.responseCode}\n" +
                    $"Message: {request.error}\n" +
                    $"Body: {request.downloadHandler.text}"
                );

                yield break;
            }

            Debug.Log("Gemini 요청 성공");

            string responseJson =
                request.downloadHandler.text;

            GeminiGenerateContentResponse response =
                JsonUtility.FromJson
                    <GeminiGenerateContentResponse>(
                        responseJson
                    );

            string analysisResult =
                ExtractResponseText(response);

            if (string.IsNullOrWhiteSpace(analysisResult))
            {
                Debug.LogWarning(
                    "Gemini 응답은 받았지만 " +
                    "분석 텍스트를 찾지 못했습니다.\n" +
                    $"Raw Response:\n{responseJson}"
                );

                yield break;
            }

            PrintCombinedAnalysis(analysisResult);
        }
    }

    /// <summary>
    /// AI 분석과 Unity 증거물 상태를 결합하여 출력한다.
    /// </summary>
    private void PrintCombinedAnalysis(
        string analysisResult
    )
    {
        if (evidenceStateCollector == null)
        {
            Debug.LogError(
                "Evidence State Collector가 " +
                "GeminiVisionClient에 연결되지 않았습니다."
            );

            return;
        }

        string evidenceSummary =
            evidenceStateCollector
                .BuildEvidenceStateSummary();

        Debug.Log(
            "=== 최종 장면 분석 테스트 ===\n\n" +
            "[AI 이미지 분석]\n" +
            analysisResult +
            "\n\n" +
            "[Unity 증거물 상태]\n" +
            evidenceSummary
        );
    }

    /// <summary>
    /// Gemini 응답에서 실제 분석 텍스트를 꺼낸다.
    /// </summary>
    private string ExtractResponseText(
        GeminiGenerateContentResponse response
    )
    {
        if (response == null)
        {
            return null;
        }

        if (response.candidates == null ||
            response.candidates.Length == 0)
        {
            return null;
        }

        GeminiCandidate firstCandidate =
            response.candidates[0];

        if (firstCandidate.content == null)
        {
            return null;
        }

        if (firstCandidate.content.parts == null ||
            firstCandidate.content.parts.Length == 0)
        {
            return null;
        }

        StringBuilder resultBuilder =
            new StringBuilder();

        foreach (
            GeminiResponsePart part
            in firstCandidate.content.parts
        )
        {
            if (!string.IsNullOrWhiteSpace(part.text))
            {
                resultBuilder.AppendLine(part.text);
            }
        }

        return resultBuilder.ToString().Trim();
    }
}

/*
 * 아래 클래스들은 Gemini의 JSON 응답을
 * Unity에서 읽기 위한 데이터 구조다.
 */

[Serializable]
public class GeminiTextPartPayload
{
    public string text;
}

[Serializable]
public class GeminiGenerateContentResponse
{
    public GeminiCandidate[] candidates;
}

[Serializable]
public class GeminiCandidate
{
    public GeminiResponseContent content;
}

[Serializable]
public class GeminiResponseContent
{
    public GeminiResponsePart[] parts;
}

[Serializable]
public class GeminiResponsePart
{
    public string text;
}