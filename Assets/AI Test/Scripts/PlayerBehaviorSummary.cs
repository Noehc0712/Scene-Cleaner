using System.Text;
using UnityEngine;

public class PlayerBehaviorSummary : MonoBehaviour
{
    [Header("행동 기록 컴포넌트")]

    [SerializeField]
    private PlayerActionRecorder actionRecorder;

    [SerializeField]
    private PlayerCameraRecorder cameraRecorder;

    [SerializeField]
    private PlayerHoverRecorder hoverRecorder;


    private void Awake()
    {
        /*
         * Inspector에서 직접 연결하지 않아도
         * 같은 GameObject에 붙어 있다면 자동으로 찾는다.
         */
        if (actionRecorder == null)
        {
            actionRecorder =
                GetComponent<PlayerActionRecorder>();
        }

        if (cameraRecorder == null)
        {
            cameraRecorder =
                GetComponent<PlayerCameraRecorder>();
        }

        if (hoverRecorder == null)
        {
            hoverRecorder =
                GetComponent<PlayerHoverRecorder>();
        }


        /*
         * 혹시 나중에 Recorder들이 다른 GameObject로
         * 이동하더라도 씬 전체에서 한 번 더 찾아본다.
         */
        if (actionRecorder == null)
        {
            actionRecorder =
                FindFirstObjectByType<PlayerActionRecorder>();
        }

        if (cameraRecorder == null)
        {
            cameraRecorder =
                FindFirstObjectByType<PlayerCameraRecorder>();
        }

        if (hoverRecorder == null)
        {
            hoverRecorder =
                FindFirstObjectByType<PlayerHoverRecorder>();
        }
    }


    /// <summary>
    /// 플레이어의 조사 행동,
    /// CCTV 관찰 시간,
    /// 마우스 관심도를 하나의 문자열로 합친다.
    ///
    /// 이후 2라운드 Gemini 프롬프트에
    /// 그대로 넣을 수 있는 데이터이다.
    /// </summary>
    public string BuildGeminiBehaviorSummary()
    {
        StringBuilder summary =
            new StringBuilder();


        summary.AppendLine(
            "=== 플레이어 행동 분석 데이터 ==="
        );

        summary.AppendLine();


        // =========================================================
        // 1. 실제 조사 행동
        // =========================================================

        summary.AppendLine(
            "[실제 조사 행동]"
        );


        if (actionRecorder != null)
        {
            summary.AppendLine(
                actionRecorder.BuildGeminiSummary()
            );
        }
        else
        {
            summary.AppendLine(
                "PlayerActionRecorder를 찾을 수 없습니다."
            );
        }


        summary.AppendLine();


        // =========================================================
        // 2. CCTV 관찰 기록
        // =========================================================

        summary.AppendLine(
            "[CCTV 관찰 기록]"
        );


        if (cameraRecorder != null)
        {
            summary.AppendLine(
                cameraRecorder.BuildGeminiSummary()
            );
        }
        else
        {
            summary.AppendLine(
                "PlayerCameraRecorder를 찾을 수 없습니다."
            );
        }


        summary.AppendLine();


        // =========================================================
        // 3. 마우스 관심도 기록
        // =========================================================

        summary.AppendLine(
            "[장소 관심도]"
        );


        if (hoverRecorder != null)
        {
            summary.AppendLine(
                hoverRecorder.BuildGeminiSummary()
            );
        }
        else
        {
            summary.AppendLine(
                "PlayerHoverRecorder를 찾을 수 없습니다."
            );
        }


        return summary.ToString();
    }


    /// <summary>
    /// 개발 중 Console에서
    /// Gemini에 전달될 전체 행동 데이터를 확인한다.
    /// </summary>
    [ContextMenu("Gemini용 전체 행동 데이터 확인")]
    private void PrintGeminiBehaviorSummaryForTest()
    {
        Debug.Log(
            BuildGeminiBehaviorSummary()
        );
    }
}