using System.Text;
using UnityEngine;

public class PlayerCameraRecorder : MonoBehaviour
{
    [Header("현재 기록 상태")]

    [SerializeField]
    private bool isRecording;

    [SerializeField]
    private int currentCameraNumber;


    [Header("카메라별 체류 시간")]

    [SerializeField]
    private float cctv01Time;

    [SerializeField]
    private float cctv02Time;

    [SerializeField]
    private float cctv03Time;

    [SerializeField]
    private float cctv04Time;


    private float lastCameraChangeTime;


    public bool IsRecording
    {
        get
        {
            return isRecording;
        }
    }


    /// <summary>
    /// CCTV 1, 2는 방으로 계산한다.
    /// </summary>
    public float RoomTime
    {
        get
        {
            return cctv01Time + cctv02Time;
        }
    }


    /// <summary>
    /// CCTV 3, 4는 주방으로 계산한다.
    /// </summary>
    public float KitchenTime
    {
        get
        {
            return cctv03Time + cctv04Time;
        }
    }


    private void OnEnable()
    {
        CCTVSwitcher.ActiveCameraChanged +=
            HandleCameraChanged;
    }


    private void OnDisable()
    {
        /*
         * 기록 중 비활성화되는 경우
         * 현재 카메라에 머문 마지막 시간까지 저장한다.
         */
        if (isRecording)
        {
            SaveCurrentCameraTime();
        }

        CCTVSwitcher.ActiveCameraChanged -=
            HandleCameraChanged;
    }


    /// <summary>
    /// CCTV가 변경됐을 때 호출된다.
    /// </summary>
    private void HandleCameraChanged(int cameraNumber)
    {
        /*
         * 아직 라운드 기록을 시작하지 않았다면
         * 현재 카메라 번호만 기억한다.
         */
        if (!isRecording)
        {
            currentCameraNumber = cameraNumber;
            return;
        }


        /*
         * 이전 카메라에서 머문 시간을 먼저 저장한다.
         */
        SaveCurrentCameraTime();


        /*
         * 새 카메라를 현재 카메라로 변경한다.
         */
        currentCameraNumber = cameraNumber;
        lastCameraChangeTime = Time.unscaledTime;
    }


    /// <summary>
    /// 실제 라운드가 시작될 때 호출한다.
    /// </summary>
    public void StartRecording()
    {
        if (isRecording)
        {
            Debug.LogWarning(
                "카메라 체류 시간 기록이 이미 진행 중입니다."
            );

            return;
        }


        isRecording = true;

        currentCameraNumber =
            CCTVSwitcher.CurrentCameraNumber;

        lastCameraChangeTime =
            Time.unscaledTime;


        Debug.Log(
            "=== CCTV 체류 시간 기록 시작 ===\n" +
            $"시작 카메라: CCTV {currentCameraNumber}"
        );
    }


    /// <summary>
    /// 라운드가 종료될 때 호출한다.
    /// </summary>
    public void StopRecording()
    {
        if (!isRecording)
        {
            Debug.LogWarning(
                "현재 카메라 체류 시간 기록이 진행 중이 아닙니다."
            );

            return;
        }


        /*
         * 마지막 카메라에서 머문 시간까지 저장한다.
         */
        SaveCurrentCameraTime();

        isRecording = false;


        Debug.Log(
            "=== CCTV 체류 시간 기록 종료 ===\n" +
            BuildGeminiSummary()
        );
    }


    /// <summary>
    /// 현재 카메라에서 지난 시간을
    /// 해당 CCTV의 누적 시간에 더한다.
    /// </summary>
    private void SaveCurrentCameraTime()
    {
        if (!isRecording)
        {
            return;
        }


        float elapsedTime =
            Time.unscaledTime - lastCameraChangeTime;


        switch (currentCameraNumber)
        {
            case 1:
                cctv01Time += elapsedTime;
                break;

            case 2:
                cctv02Time += elapsedTime;
                break;

            case 3:
                cctv03Time += elapsedTime;
                break;

            case 4:
                cctv04Time += elapsedTime;
                break;
        }


        lastCameraChangeTime =
            Time.unscaledTime;
    }


    /// <summary>
    /// 현재까지 기록된 카메라 체류 시간을
    /// Gemini에게 전달할 수 있는 문장으로 만든다.
    /// </summary>
    public string BuildGeminiSummary()
    {
        /*
         * 기록 도중 Summary를 요청해도
         * 현재 진행 중인 시간을 포함해서 보여준다.
         */
        float tempCctv01 = cctv01Time;
        float tempCctv02 = cctv02Time;
        float tempCctv03 = cctv03Time;
        float tempCctv04 = cctv04Time;


        if (isRecording)
        {
            float elapsedTime =
                Time.unscaledTime - lastCameraChangeTime;


            switch (currentCameraNumber)
            {
                case 1:
                    tempCctv01 += elapsedTime;
                    break;

                case 2:
                    tempCctv02 += elapsedTime;
                    break;

                case 3:
                    tempCctv03 += elapsedTime;
                    break;

                case 4:
                    tempCctv04 += elapsedTime;
                    break;
            }
        }


        float roomTime =
            tempCctv01 + tempCctv02;

        float kitchenTime =
            tempCctv03 + tempCctv04;


        StringBuilder summary =
            new StringBuilder();


        summary.AppendLine(
            "플레이어의 CCTV 관찰 기록:"
        );

        summary.AppendLine(
            $"- CCTV 1 체류 시간: {tempCctv01:F1}초"
        );

        summary.AppendLine(
            $"- CCTV 2 체류 시간: {tempCctv02:F1}초"
        );

        summary.AppendLine(
            $"- CCTV 3 체류 시간: {tempCctv03:F1}초"
        );

        summary.AppendLine(
            $"- CCTV 4 체류 시간: {tempCctv04:F1}초"
        );

        summary.AppendLine(
            $"- 방 전체 체류 시간: {roomTime:F1}초"
        );

        summary.AppendLine(
            $"- 주방 전체 체류 시간: {kitchenTime:F1}초"
        );


        return summary.ToString();
    }


    /// <summary>
    /// 새 게임을 시작할 때 모든 체류 시간을 초기화한다.
    /// </summary>
    public void ClearRecords()
    {
        isRecording = false;

        currentCameraNumber =
            CCTVSwitcher.CurrentCameraNumber;

        cctv01Time = 0f;
        cctv02Time = 0f;
        cctv03Time = 0f;
        cctv04Time = 0f;

        lastCameraChangeTime =
            Time.unscaledTime;


        Debug.Log(
            "CCTV 체류 시간 기록을 모두 초기화했습니다."
        );
    }


    // =============================================================
    // 아래 4개는 현재 개발 단계에서 직접 테스트하기 위한 메뉴
    // =============================================================

    [ContextMenu("CCTV 체류 시간 기록 시작")]
    private void StartRecordingForTest()
    {
        StartRecording();
    }


    [ContextMenu("CCTV 체류 시간 기록 종료")]
    private void StopRecordingForTest()
    {
        StopRecording();
    }


    [ContextMenu("Gemini용 CCTV 기록 확인")]
    private void PrintGeminiSummaryForTest()
    {
        Debug.Log(
            BuildGeminiSummary()
        );
    }


    [ContextMenu("CCTV 기록 초기화")]
    private void ClearRecordsForTest()
    {
        ClearRecords();
    }
}