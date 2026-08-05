using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class AIHideController : MonoBehaviour
{
    [Header("수동 테스트용")]

    [SerializeField]
    private EvidenceObject targetEvidence;

    [SerializeField]
    private string targetHideZoneId = "hide_zone_02";

    /// <summary>
    /// Gemini가 만든 전체 숨김 계획을 검사한 뒤
    /// 씬의 증거물에 실제로 적용한다.
    /// </summary>
    public bool ApplyHidePlan(AIHidePlan plan)
    {
        if (plan == null)
        {
            Debug.LogError(
                "적용할 AI 숨김 계획이 없습니다."
            );

            return false;
        }

        if (plan.assignments == null ||
            plan.assignments.Length == 0)
        {
            Debug.LogError(
                "AI 숨김 계획에 배치 정보가 없습니다."
            );

            return false;
        }

        EvidenceObject[] evidenceObjects =
            FindObjectsByType<EvidenceObject>(
                FindObjectsSortMode.None
            );

        HideZone[] hideZones =
            FindObjectsByType<HideZone>(
                FindObjectsSortMode.None
            );

        Dictionary<string, EvidenceObject> evidenceById =
            BuildEvidenceDictionary(evidenceObjects);

        Dictionary<string, HideZone> hideZoneById =
            BuildHideZoneDictionary(hideZones);

        if (evidenceById == null ||
            hideZoneById == null)
        {
            return false;
        }

        if (!ValidatePlan(
                plan,
                evidenceById,
                hideZoneById
            ))
        {
            return false;
        }

        StringBuilder resultBuilder =
            new StringBuilder();

        resultBuilder.AppendLine(
            "=== Gemini 숨김 계획 적용 결과 ==="
        );

        foreach (
            AIHideAssignment assignment
            in plan.assignments
        )
        {
            EvidenceObject evidence =
                evidenceById[assignment.evidenceId];

            HideZone zone =
                hideZoneById[assignment.hideZoneId];

            bool hideSucceeded =
                evidence.HideAt(zone);

            if (!hideSucceeded)
            {
                Debug.LogError(
                    "Gemini 숨김 계획 적용 중 실패했습니다.\n" +
                    $"물건 ID: {assignment.evidenceId}\n" +
                    $"장소 ID: {assignment.hideZoneId}"
                );

                return false;
            }

            resultBuilder.AppendLine(
                $"- {evidence.EvidenceName}"
            );

            resultBuilder.AppendLine(
                $"  선택 장소: {zone.ZoneName}"
            );

            resultBuilder.AppendLine(
                $"  장소 ID: {zone.ZoneId}"
            );
        }

        Debug.Log(
            resultBuilder.ToString().Trim()
        );

        return true;
    }

    /// <summary>
    /// 물건 ID와 장소 ID를 전달받아
    /// 해당 물건 하나를 숨긴다.
    /// </summary>
    public bool HideEvidenceByIds(
        string evidenceId,
        string hideZoneId
    )
    {
        EvidenceObject evidence =
            FindEvidenceById(evidenceId);

        if (evidence == null)
        {
            Debug.LogError(
                $"ID가 '{evidenceId}'인 " +
                "EvidenceObject를 찾지 못했습니다."
            );

            return false;
        }

        return HideEvidenceByZoneId(
            evidence,
            hideZoneId
        );
    }

    /// <summary>
    /// 지정한 EvidenceObject를
    /// HideZone ID에 해당하는 위치로 숨긴다.
    /// </summary>
    public bool HideEvidenceByZoneId(
        EvidenceObject evidence,
        string hideZoneId
    )
    {
        if (evidence == null)
        {
            Debug.LogError(
                "숨길 EvidenceObject가 연결되지 않았습니다."
            );

            return false;
        }

        if (string.IsNullOrWhiteSpace(hideZoneId))
        {
            Debug.LogError(
                "숨김 장소 ID가 비어 있습니다."
            );

            return false;
        }

        HideZone targetZone =
            FindHideZoneById(hideZoneId);

        if (targetZone == null)
        {
            Debug.LogError(
                $"ID가 '{hideZoneId}'인 " +
                "HideZone을 찾지 못했습니다."
            );

            return false;
        }

        bool hideSucceeded =
            evidence.HideAt(targetZone);

        if (!hideSucceeded)
        {
            Debug.LogError(
                $"{evidence.EvidenceName} 숨기기에 실패했습니다."
            );

            return false;
        }

        Debug.Log(
            "=== 숨김 장소 ID 적용 성공 ===\n" +
            $"증거물: {evidence.EvidenceName}\n" +
            $"전달받은 ID: {hideZoneId}\n" +
            $"선택된 장소: {targetZone.ZoneName}"
        );

        return true;
    }

    /// <summary>
    /// 씬의 증거물을 Evidence ID 기준으로 정리한다.
    /// </summary>
    private Dictionary<string, EvidenceObject>
        BuildEvidenceDictionary(
            EvidenceObject[] evidenceObjects
        )
    {
        Dictionary<string, EvidenceObject> result =
            new Dictionary<string, EvidenceObject>(
                StringComparer.Ordinal
            );

        foreach (
            EvidenceObject evidence
            in evidenceObjects
        )
        {
            if (string.IsNullOrWhiteSpace(
                    evidence.EvidenceId
                ))
            {
                Debug.LogError(
                    $"{evidence.gameObject.name}의 " +
                    "Evidence ID가 비어 있습니다."
                );

                return null;
            }

            if (result.ContainsKey(
                    evidence.EvidenceId
                ))
            {
                Debug.LogError(
                    "중복된 Evidence ID가 있습니다: " +
                    evidence.EvidenceId
                );

                return null;
            }

            result.Add(
                evidence.EvidenceId,
                evidence
            );
        }

        return result;
    }

    /// <summary>
    /// 씬의 숨김 장소를 Zone ID 기준으로 정리한다.
    /// </summary>
    private Dictionary<string, HideZone>
        BuildHideZoneDictionary(
            HideZone[] hideZones
        )
    {
        Dictionary<string, HideZone> result =
            new Dictionary<string, HideZone>(
                StringComparer.Ordinal
            );

        foreach (HideZone zone in hideZones)
        {
            if (string.IsNullOrWhiteSpace(
                    zone.ZoneId
                ))
            {
                Debug.LogError(
                    $"{zone.gameObject.name}의 " +
                    "Zone ID가 비어 있습니다."
                );

                return null;
            }

            if (result.ContainsKey(zone.ZoneId))
            {
                Debug.LogError(
                    "중복된 HideZone ID가 있습니다: " +
                    zone.ZoneId
                );

                return null;
            }

            result.Add(
                zone.ZoneId,
                zone
            );
        }

        return result;
    }

    /// <summary>
    /// AI가 반환한 ID들이 실제로 존재하는지,
    /// 같은 물건이나 장소가 중복되지 않았는지 검사한다.
    /// </summary>
    private bool ValidatePlan(
        AIHidePlan plan,
        Dictionary<string, EvidenceObject> evidenceById,
        Dictionary<string, HideZone> hideZoneById
    )
    {
        if (plan.assignments.Length !=
            evidenceById.Count)
        {
            Debug.LogError(
                "Gemini가 모든 물건의 숨김 장소를 " +
                "지정하지 않았습니다.\n" +
                $"씬의 물건 수: {evidenceById.Count}\n" +
                $"응답의 배치 수: {plan.assignments.Length}"
            );

            return false;
        }

        HashSet<string> usedEvidenceIds =
            new HashSet<string>(
                StringComparer.Ordinal
            );

        HashSet<string> usedHideZoneIds =
            new HashSet<string>(
                StringComparer.Ordinal
            );

        foreach (
            AIHideAssignment assignment
            in plan.assignments
        )
        {
            if (assignment == null)
            {
                Debug.LogError(
                    "Gemini 응답에 비어 있는 배치 정보가 있습니다."
                );

                return false;
            }

            if (string.IsNullOrWhiteSpace(
                    assignment.evidenceId
                ))
            {
                Debug.LogError(
                    "Gemini 응답의 evidenceId가 비어 있습니다."
                );

                return false;
            }

            if (string.IsNullOrWhiteSpace(
                    assignment.hideZoneId
                ))
            {
                Debug.LogError(
                    "Gemini 응답의 hideZoneId가 비어 있습니다."
                );

                return false;
            }

            if (!evidenceById.ContainsKey(
                    assignment.evidenceId
                ))
            {
                Debug.LogError(
                    "Gemini가 존재하지 않는 " +
                    "Evidence ID를 반환했습니다: " +
                    assignment.evidenceId
                );

                return false;
            }

            if (!hideZoneById.ContainsKey(
                    assignment.hideZoneId
                ))
            {
                Debug.LogError(
                    "Gemini가 존재하지 않는 " +
                    "HideZone ID를 반환했습니다: " +
                    assignment.hideZoneId
                );

                return false;
            }

            if (!usedEvidenceIds.Add(
                    assignment.evidenceId
                ))
            {
                Debug.LogError(
                    "Gemini가 같은 물건을 " +
                    "두 번 배치했습니다: " +
                    assignment.evidenceId
                );

                return false;
            }

            if (!usedHideZoneIds.Add(
                    assignment.hideZoneId
                ))
            {
                Debug.LogError(
                    "Gemini가 같은 숨김 장소를 " +
                    "두 번 선택했습니다: " +
                    assignment.hideZoneId
                );

                return false;
            }

            HideZone selectedZone =
                hideZoneById[
                    assignment.hideZoneId
                ];

            if (selectedZone.IsOccupied)
            {
                Debug.LogError(
                    "Gemini가 이미 사용 중인 장소를 " +
                    "선택했습니다: " +
                    selectedZone.ZoneId
                );

                return false;
            }
        }

        return true;
    }

    private EvidenceObject FindEvidenceById(
        string evidenceId
    )
    {
        EvidenceObject[] evidenceObjects =
            FindObjectsByType<EvidenceObject>(
                FindObjectsSortMode.None
            );

        foreach (
            EvidenceObject evidence
            in evidenceObjects
        )
        {
            if (evidence.EvidenceId == evidenceId)
            {
                return evidence;
            }
        }

        return null;
    }

    private HideZone FindHideZoneById(
        string hideZoneId
    )
    {
        HideZone[] hideZones =
            FindObjectsByType<HideZone>(
                FindObjectsSortMode.None
            );

        foreach (HideZone zone in hideZones)
        {
            if (zone.ZoneId == hideZoneId)
            {
                return zone;
            }
        }

        return null;
    }

    /// <summary>
    /// 기존의 수동 ID 테스트 기능이다.
    /// </summary>
    [ContextMenu("ID로 증거물 숨기기 테스트")]
    private void HideEvidenceForTest()
    {
        HideEvidenceByZoneId(
            targetEvidence,
            targetHideZoneId
        );
    }
}