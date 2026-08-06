using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum InvestigationResultType
{
    Clue,
    Decoy,
    Empty
}

[Serializable]
public class PlayerInvestigationRecord
{
    public int order;
    public int roundNumber;
    public string hideZoneId;
    public string hideZoneName;
    public InvestigationResultType resultType;
}

public class PlayerActionRecorder : MonoBehaviour
{
    [Header("현재 라운드")]

    [SerializeField, Min(1)]
    private int currentRound = 1;

    [Header("플레이어 조사 기록")]

    [SerializeField]
    private List<PlayerInvestigationRecord> investigationRecords =
        new List<PlayerInvestigationRecord>();

    [Header("수동 테스트용")]

    [SerializeField]
    private HideZone testHideZone;

    [SerializeField]
    private InvestigationResultType testResultType =
        InvestigationResultType.Empty;

    public int CurrentRound
    {
        get
        {
            return currentRound;
        }
    }

    public IReadOnlyList<PlayerInvestigationRecord> InvestigationRecords
    {
        get
        {
            return investigationRecords;
        }
    }

    /// <summary>
    /// 플레이어가 조사한 장소와 조사 결과를 순서대로 저장한다.
    /// </summary>
    public bool RecordInvestigation(
        HideZone hideZone,
        InvestigationResultType resultType
    )
    {
        if (hideZone == null)
        {
            Debug.LogError(
                "조사 기록을 저장할 HideZone이 없습니다."
            );

            return false;
        }

        PlayerInvestigationRecord newRecord =
            new PlayerInvestigationRecord
            {
                order = investigationRecords.Count + 1,
                roundNumber = currentRound,
                hideZoneId = hideZone.ZoneId,
                hideZoneName = hideZone.ZoneName,
                resultType = resultType
            };

        investigationRecords.Add(newRecord);

        Debug.Log(
            "=== 플레이어 조사 행동 기록 ===\n" +
            $"라운드: {newRecord.roundNumber}\n" +
            $"조사 순서: {newRecord.order}\n" +
            $"장소: {newRecord.hideZoneName}\n" +
            $"장소 ID: {newRecord.hideZoneId}\n" +
            $"조사 결과: {newRecord.resultType}"
        );

        return true;
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
            $"현재 기록 라운드를 {currentRound}라운드로 변경했습니다."
        );
    }

    /// <summary>
    /// 저장된 기록을 Gemini 프롬프트에 넣을 수 있는
    /// 텍스트 형태로 변환한다.
    /// </summary>
    public string BuildGeminiSummary()
    {
        if (investigationRecords.Count == 0)
        {
            return "플레이어 조사 기록이 없습니다.";
        }

        StringBuilder summary = new StringBuilder();

        summary.AppendLine("플레이어의 조사 행동 기록:");

        foreach (PlayerInvestigationRecord record in investigationRecords)
        {
            summary.AppendLine(
                $"- {record.order}번째 조사: " +
                $"{record.hideZoneName} " +
                $"({record.hideZoneId}), " +
                $"결과: {record.resultType}"
            );
        }

        return summary.ToString();
    }

    /// <summary>
    /// 모든 플레이어 조사 기록을 삭제한다.
    /// 새 게임을 시작할 때 사용한다.
    /// </summary>
    public void ClearRecords()
    {
        investigationRecords.Clear();

        Debug.Log(
            "플레이어 조사 기록을 모두 초기화했습니다."
        );
    }

    [ContextMenu("조사 행동 기록 테스트")]
    private void RecordInvestigationForTest()
    {
        RecordInvestigation(
            testHideZone,
            testResultType
        );
    }

    [ContextMenu("Gemini용 행동 기록 확인")]
    private void PrintGeminiSummaryForTest()
    {
        Debug.Log(BuildGeminiSummary());
    }

    [ContextMenu("플레이어 행동 기록 초기화")]
    private void ClearRecordsForTest()
    {
        ClearRecords();
    }
}