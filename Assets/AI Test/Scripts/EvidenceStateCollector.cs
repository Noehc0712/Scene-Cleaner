using System.Text;
using UnityEngine;

public class EvidenceStateCollector : MonoBehaviour
{
    public string BuildEvidenceStateSummary()
    {
        EvidenceObject[] evidenceObjects =
            FindObjectsByType<EvidenceObject>(
                FindObjectsSortMode.None
            );

        if (evidenceObjects.Length == 0)
        {
            return "현재 씬에 등록된 증거물이 없습니다.";
        }

        StringBuilder summaryBuilder =
            new StringBuilder();

        summaryBuilder.AppendLine(
            "=== 현재 증거물 상태 ==="
        );

        foreach (EvidenceObject evidence in evidenceObjects)
        {
            string hiddenState =
                evidence.IsHidden ? "숨김" : "노출";

            summaryBuilder.AppendLine(
                $"- {evidence.EvidenceName}: {hiddenState}"
            );
        }

        return summaryBuilder.ToString().Trim();
    }

    [ContextMenu("증거물 상태 출력 테스트")]
    private void PrintEvidenceStateForTest()
    {
        string summary =
            BuildEvidenceStateSummary();

        Debug.Log(summary);
    }
}