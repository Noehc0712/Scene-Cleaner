using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


[Serializable]
public class PlayerHoverRecord
{
    public int roundNumber;

    public string hideZoneId;

    public string hideZoneName;

    public int hoverCount;

    public float totalHoverTime;
}


public class PlayerHoverRecorder : MonoBehaviour
{
    [Header("현재 라운드")]

    [SerializeField, Min(1)]
    private int currentRound = 1;


    [Header("현재 기록 상태")]

    [SerializeField]
    private bool isRecording;


    [SerializeField]
    private CCTVInteractable currentHoveredInteractable;


    [Header("플레이어 관심 기록")]

    [SerializeField]
    private List<PlayerHoverRecord> hoverRecords =
        new List<PlayerHoverRecord>();


    private float hoverStartTime;


    public bool IsRecording
    {
        get
        {
            return isRecording;
        }
    }


    public IReadOnlyList<PlayerHoverRecord> HoverRecords
    {
        get
        {
            return hoverRecords;
        }
    }


    private void OnEnable()
    {
        CCTVCursorInteractor.HoveredInteractableChanged +=
            HandleHoveredInteractableChanged;
    }


    private void OnDisable()
    {
        if (isRecording)
        {
            SaveCurrentHoverTime();
        }


        CCTVCursorInteractor.HoveredInteractableChanged -=
            HandleHoveredInteractableChanged;
    }


    /// <summary>
    /// 실제 라운드가 시작될 때 호출한다.
    /// </summary>
    public void StartRecording()
    {
        if (isRecording)
        {
            Debug.LogWarning(
                "마우스 관심도 기록이 이미 진행 중입니다."
            );

            return;
        }


        isRecording = true;


        currentHoveredInteractable =
            CCTVCursorInteractor
                .CurrentHoveredInteractable;


        hoverStartTime =
            Time.unscaledTime;


        /*
         * 기록 시작 순간부터 이미 어떤 물건 위에
         * 마우스가 있었다면 Hover 1회로 기록한다.
         */
        if (currentHoveredInteractable != null)
        {
            IncreaseHoverCount(
                currentHoveredInteractable
            );
        }


        Debug.Log(
            "=== 플레이어 마우스 관심도 기록 시작 ==="
        );
    }


    /// <summary>
    /// 라운드 종료 시 호출한다.
    /// </summary>
    public void StopRecording()
    {
        if (!isRecording)
        {
            Debug.LogWarning(
                "현재 마우스 관심도 기록이 진행 중이 아닙니다."
            );

            return;
        }


        /*
         * 마지막으로 보고 있던 물건의 시간까지 저장
         */
        SaveCurrentHoverTime();


        isRecording = false;

        currentHoveredInteractable = null;


        Debug.Log(
            "=== 플레이어 마우스 관심도 기록 종료 ===\n" +
            BuildGeminiSummary()
        );
    }


    /// <summary>
    /// 마우스가 다른 조사 대상으로 이동했을 때 호출된다.
    /// </summary>
    private void HandleHoveredInteractableChanged(
        CCTVInteractable newInteractable
    )
    {
        if (!isRecording)
        {
            currentHoveredInteractable =
                newInteractable;

            return;
        }


        /*
         * 이전 물건을 보고 있던 시간을 저장한다.
         */
        SaveCurrentHoverTime();


        /*
         * 새 Hover 대상으로 변경한다.
         */
        currentHoveredInteractable =
            newInteractable;

        hoverStartTime =
            Time.unscaledTime;


        /*
         * 실제 조사 가능한 물건 위에 들어갔다면
         * Hover 횟수를 1 증가시킨다.
         */
        if (currentHoveredInteractable != null)
        {
            IncreaseHoverCount(
                currentHoveredInteractable
            );
        }
    }


    /// <summary>
    /// 현재 Hover 중인 장소에서 머문 시간을 저장한다.
    /// </summary>
    private void SaveCurrentHoverTime()
    {
        if (!isRecording ||
            currentHoveredInteractable == null)
        {
            return;
        }


        float elapsedTime =
            Time.unscaledTime - hoverStartTime;


        HideZone hideZone =
            currentHoveredInteractable
                .GetComponent<HideZone>();


        if (hideZone == null)
        {
            Debug.LogWarning(
                "Hover 기록 실패:\n" +
                $"{currentHoveredInteractable.gameObject.name}에 " +
                "HideZone이 없습니다."
            );

            return;
        }


        PlayerHoverRecord record =
            FindOrCreateRecord(hideZone);


        record.totalHoverTime +=
            elapsedTime;
    }


    /// <summary>
    /// 해당 장소 위에 마우스가 새로 올라왔을 때
    /// 관심 횟수를 증가시킨다.
    /// </summary>
    private void IncreaseHoverCount(
        CCTVInteractable interactable
    )
    {
        if (interactable == null)
        {
            return;
        }


        HideZone hideZone =
            interactable.GetComponent<HideZone>();


        if (hideZone == null)
        {
            return;
        }


        PlayerHoverRecord record =
            FindOrCreateRecord(hideZone);


        record.hoverCount++;
    }


    /// <summary>
    /// 해당 HideZone의 기존 기록을 찾는다.
    /// 기록이 없다면 새로 만든다.
    /// </summary>
    private PlayerHoverRecord FindOrCreateRecord(
        HideZone hideZone
    )
    {
        foreach (PlayerHoverRecord record in hoverRecords)
        {
            if (record.roundNumber == currentRound &&
                record.hideZoneId == hideZone.ZoneId)
            {
                return record;
            }
        }


        PlayerHoverRecord newRecord =
            new PlayerHoverRecord
            {
                roundNumber = currentRound,
                hideZoneId = hideZone.ZoneId,
                hideZoneName = hideZone.ZoneName,
                hoverCount = 0,
                totalHoverTime = 0f
            };


        hoverRecords.Add(newRecord);


        return newRecord;
    }


    /// <summary>
    /// 현재 라운드 번호를 변경한다.
    /// </summary>
    public void SetCurrentRound(int roundNumber)
    {
        if (roundNumber < 1)
        {
            Debug.LogError(
                "라운드 번호는 1 이상이어야 합니다."
            );

            return;
        }


        currentRound = roundNumber;


        Debug.Log(
            $"마우스 관심도 기록 라운드를 " +
            $"{currentRound}라운드로 변경했습니다."
        );
    }


    /// <summary>
    /// Gemini에게 전달할 수 있는
    /// 플레이어 관심도 문장으로 변환한다.
    /// </summary>
    public string BuildGeminiSummary()
    {
        if (hoverRecords.Count == 0)
        {
            return "플레이어의 마우스 관심 기록이 없습니다.";
        }


        StringBuilder summary =
            new StringBuilder();


        summary.AppendLine(
            "플레이어의 조사 장소 관심 기록:"
        );


        foreach (PlayerHoverRecord record in hoverRecords)
        {
            summary.AppendLine(
                $"- {record.hideZoneName} " +
                $"({record.hideZoneId}): " +
                $"마우스 진입 {record.hoverCount}회, " +
                $"총 관심 시간 {record.totalHoverTime:F1}초"
            );
        }


        return summary.ToString();
    }


    /// <summary>
    /// 새 게임 시작 시 모든 Hover 기록을 삭제한다.
    /// </summary>
    public void ClearRecords()
    {
        isRecording = false;

        currentHoveredInteractable = null;

        hoverRecords.Clear();


        Debug.Log(
            "플레이어 마우스 관심 기록을 모두 초기화했습니다."
        );
    }


    // =============================================================
    // 개발 단계 테스트용 Context Menu
    // =============================================================

    [ContextMenu("마우스 관심도 기록 시작")]
    private void StartRecordingForTest()
    {
        StartRecording();
    }


    [ContextMenu("마우스 관심도 기록 종료")]
    private void StopRecordingForTest()
    {
        StopRecording();
    }


    [ContextMenu("Gemini용 마우스 기록 확인")]
    private void PrintGeminiSummaryForTest()
    {
        Debug.Log(
            BuildGeminiSummary()
        );
    }


    [ContextMenu("마우스 관심 기록 초기화")]
    private void ClearRecordsForTest()
    {
        ClearRecords();
    }
}