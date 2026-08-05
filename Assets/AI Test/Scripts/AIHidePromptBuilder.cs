using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class AIHidePromptBuilder : MonoBehaviour
{
    [Header("필요한 증거물 구성")]

    [SerializeField]
    private int requiredClueCount = 4;

    [SerializeField]
    private int requiredDecoyCount = 2;

    /// <summary>
    /// 현재 씬에 있는 EvidenceObject와 HideZone 정보를 모아서
    /// Gemini에게 보낼 숨김 계획 프롬프트를 만든다.
    /// </summary>
    public string BuildPrompt()
    {
        EvidenceObject[] evidenceObjects =
            FindObjectsByType<EvidenceObject>(
                FindObjectsSortMode.None
            );

        HideZone[] hideZones =
            FindObjectsByType<HideZone>(
                FindObjectsSortMode.None
            );

        /*
         * 출력 결과가 매번 같은 순서로 나오도록
         * ID 기준으로 정렬한다.
         */
        Array.Sort(
            evidenceObjects,
            (left, right) =>
                string.Compare(
                    left.EvidenceId,
                    right.EvidenceId,
                    StringComparison.Ordinal
                )
        );

        Array.Sort(
            hideZones,
            (left, right) =>
                string.Compare(
                    left.ZoneId,
                    right.ZoneId,
                    StringComparison.Ordinal
                )
        );

        /*
         * EvidenceObject가 하나도 없으면
         * 프롬프트를 만들 수 없다.
         */
        if (evidenceObjects.Length == 0)
        {
            Debug.LogError(
                "씬에서 EvidenceObject를 찾지 못했습니다."
            );

            return string.Empty;
        }

        /*
         * HideZone이 하나도 없으면
         * 물건을 숨길 장소가 없다.
         */
        if (hideZones.Length == 0)
        {
            Debug.LogError(
                "씬에서 HideZone을 찾지 못했습니다."
            );

            return string.Empty;
        }

        /*
         * 증거물 ID와 HideZone ID가 비어 있거나
         * 중복되어 있는지 확인한다.
         */
        if (!ValidateEvidenceIds(evidenceObjects))
        {
            return string.Empty;
        }

        if (!ValidateHideZoneIds(hideZones))
        {
            return string.Empty;
        }

        int clueCount = 0;
        int decoyCount = 0;

        int physicalObjectCount = 0;
        int surfaceTraceCount = 0;

        foreach (EvidenceObject evidence in evidenceObjects)
        {
            if (evidence.EvidenceType == EvidenceType.Clue)
            {
                clueCount++;
            }
            else if (
                evidence.EvidenceType == EvidenceType.Decoy
            )
            {
                decoyCount++;
            }

            if (
                evidence.PlacementType ==
                EvidencePlacementType.PhysicalObject
            )
            {
                physicalObjectCount++;
            }
            else if (
                evidence.PlacementType ==
                EvidencePlacementType.SurfaceTrace
            )
            {
                surfaceTraceCount++;
            }
        }

        /*
         * 현재 테스트에서 정한 구성은
         * Clue 4개, Decoy 2개다.
         *
         * 개수가 다르더라도 프롬프트는 만들지만,
         * Console에 경고를 출력한다.
         */
        if (clueCount != requiredClueCount)
        {
            Debug.LogWarning(
                "Clue 개수가 예상과 다릅니다.\n" +
                $"현재 개수: {clueCount}\n" +
                $"예상 개수: {requiredClueCount}"
            );
        }

        if (decoyCount != requiredDecoyCount)
        {
            Debug.LogWarning(
                "Decoy 개수가 예상과 다릅니다.\n" +
                $"현재 개수: {decoyCount}\n" +
                $"예상 개수: {requiredDecoyCount}"
            );
        }

        /*
         * 이미 다른 증거물이 사용하고 있는 HideZone은 제외한다.
         */
        List<HideZone> availableHideZones =
            new List<HideZone>();

        foreach (HideZone zone in hideZones)
        {
            if (!zone.IsOccupied)
            {
                availableHideZones.Add(zone);
            }
        }

        /*
         * 증거물 수보다 사용 가능한 장소 수가 적으면
         * 모든 증거물을 서로 다른 장소에 넣을 수 없다.
         */
        if (
            availableHideZones.Count <
            evidenceObjects.Length
        )
        {
            Debug.LogError(
                "사용 가능한 HideZone의 수가 부족합니다.\n" +
                $"증거물 수: {evidenceObjects.Length}\n" +
                $"사용 가능한 HideZone 수: " +
                $"{availableHideZones.Count}"
            );

            return string.Empty;
        }

        int physicalZoneCount = 0;
        int surfaceTraceZoneCount = 0;

        foreach (HideZone zone in availableHideZones)
        {
            if (zone.AllowPhysicalObject)
            {
                physicalZoneCount++;
            }

            if (zone.AllowSurfaceTrace)
            {
                surfaceTraceZoneCount++;
            }
        }

        /*
         * 물건형 증거물보다 물건형 허용 장소가 적으면
         * 올바른 배치 계획을 만들 수 없다.
         */
        if (physicalZoneCount < physicalObjectCount)
        {
            Debug.LogError(
                "PhysicalObject를 배치할 장소가 부족합니다.\n" +
                $"PhysicalObject 수: {physicalObjectCount}\n" +
                $"허용 장소 수: {physicalZoneCount}"
            );

            return string.Empty;
        }

        /*
         * 흔적형 증거물보다 흔적형 허용 장소가 적으면
         * 올바른 배치 계획을 만들 수 없다.
         */
        if (surfaceTraceZoneCount < surfaceTraceCount)
        {
            Debug.LogError(
                "SurfaceTrace를 배치할 장소가 부족합니다.\n" +
                $"SurfaceTrace 수: {surfaceTraceCount}\n" +
                $"허용 장소 수: {surfaceTraceZoneCount}"
            );

            return string.Empty;
        }

        StringBuilder prompt = new StringBuilder();

        prompt.AppendLine(
            "당신은 3D 범죄 현장 위장 퍼즐 게임의 " +
            "증거물 숨김 계획 AI입니다."
        );

        prompt.AppendLine();

        prompt.AppendLine(
            "아래 증거물들을 사용 가능한 숨김 장소에 " +
            "하나씩 배치하세요."
        );

        prompt.AppendLine(
            "각 증거물의 의미, 배치 형태, 장소의 난이도와 " +
            "허용 배치 형태를 고려하세요."
        );

        prompt.AppendLine();

        prompt.AppendLine("=== 증거물 목록 ===");

        foreach (EvidenceObject evidence in evidenceObjects)
        {
            prompt.AppendLine(
                $"- 증거물 ID: {evidence.EvidenceId}"
            );

            prompt.AppendLine(
                $"  이름: {evidence.EvidenceName}"
            );

            prompt.AppendLine(
                $"  정답 구분: {evidence.EvidenceType}"
            );

            prompt.AppendLine(
                $"  배치 형태: {evidence.PlacementType}"
            );

            prompt.AppendLine(
                $"  설명: {evidence.EvidenceDescription}"
            );

            prompt.AppendLine();
        }

        prompt.AppendLine("=== 사용 가능한 숨김 장소 목록 ===");

        foreach (HideZone zone in availableHideZones)
        {
            string allowedPlacementTypes =
                GetAllowedPlacementTypes(zone);

            prompt.AppendLine(
                $"- 장소 ID: {zone.ZoneId}"
            );

            prompt.AppendLine(
                $"  장소 이름: {zone.ZoneName}"
            );

            prompt.AppendLine(
                $"  난이도: {zone.Difficulty}"
            );

            prompt.AppendLine(
                $"  허용 배치 형태: {allowedPlacementTypes}"
            );

            prompt.AppendLine();
        }

        prompt.AppendLine("=== 배치 규칙 ===");

        prompt.AppendLine(
            "1. 모든 증거물을 정확히 한 번씩 배치하세요."
        );

        prompt.AppendLine(
            "2. 하나의 숨김 장소에는 증거물 하나만 배치하세요."
        );

        prompt.AppendLine(
            "3. 같은 장소 ID를 두 번 이상 사용하지 마세요."
        );

        prompt.AppendLine(
            "4. 목록에 존재하는 증거물 ID와 장소 ID만 사용하세요."
        );

        prompt.AppendLine(
            "5. 증거물 ID와 장소 ID의 철자나 형식을 변경하지 마세요."
        );

        prompt.AppendLine(
            "6. 증거물의 특징과 장소의 의미가 자연스럽게 " +
            "어울리도록 선택하세요."
        );

        prompt.AppendLine(
            "7. Clue와 Decoy가 모두 너무 비슷한 난이도의 장소에 " +
            "몰리지 않도록 적절히 분산하세요."
        );

        prompt.AppendLine(
            "8. 각 증거물의 배치 형태와 장소의 허용 배치 형태가 " +
            "반드시 일치해야 합니다."
        );

        prompt.AppendLine(
            "9. PhysicalObject는 PhysicalObject를 허용하는 " +
            "장소에만 배치하세요."
        );

        prompt.AppendLine(
            "10. SurfaceTrace는 SurfaceTrace를 허용하는 " +
            "장소에만 배치하세요."
        );

        prompt.AppendLine();

        prompt.AppendLine("=== 응답 형식 ===");

        prompt.AppendLine(
            "설명이나 마크다운 코드 블록을 작성하지 말고, " +
            "아래 구조의 JSON만 반환하세요."
        );

        prompt.AppendLine();

        prompt.AppendLine("{");
        prompt.AppendLine("  \"assignments\": [");
        prompt.AppendLine("    {");
        prompt.AppendLine(
            "      \"evidenceId\": \"증거물 ID\","
        );
        prompt.AppendLine(
            "      \"hideZoneId\": \"장소 ID\""
        );
        prompt.AppendLine("    }");
        prompt.AppendLine("  ]");
        prompt.AppendLine("}");

        return prompt.ToString();
    }

    /// <summary>
    /// HideZone이 허용하는 배치 형태를
    /// Gemini가 읽을 문자열로 만든다.
    /// </summary>
    private string GetAllowedPlacementTypes(
        HideZone zone
    )
    {
        List<string> allowedTypes =
            new List<string>();

        if (zone.AllowPhysicalObject)
        {
            allowedTypes.Add(
                EvidencePlacementType
                    .PhysicalObject
                    .ToString()
            );
        }

        if (zone.AllowSurfaceTrace)
        {
            allowedTypes.Add(
                EvidencePlacementType
                    .SurfaceTrace
                    .ToString()
            );
        }

        if (allowedTypes.Count == 0)
        {
            return "없음";
        }

        return string.Join(", ", allowedTypes);
    }

    /// <summary>
    /// EvidenceObject의 ID가 비어 있거나
    /// 중복되었는지 검사한다.
    /// </summary>
    private bool ValidateEvidenceIds(
        EvidenceObject[] evidenceObjects
    )
    {
        HashSet<string> usedIds =
            new HashSet<string>();

        foreach (EvidenceObject evidence in evidenceObjects)
        {
            if (
                string.IsNullOrWhiteSpace(
                    evidence.EvidenceId
                )
            )
            {
                Debug.LogError(
                    $"{evidence.gameObject.name}의 " +
                    "Evidence Id가 비어 있습니다."
                );

                return false;
            }

            if (!usedIds.Add(evidence.EvidenceId))
            {
                Debug.LogError(
                    "중복된 Evidence Id가 있습니다.\n" +
                    $"중복 ID: {evidence.EvidenceId}"
                );

                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// HideZone의 ID가 비어 있거나
    /// 중복되었는지 검사한다.
    /// </summary>
    private bool ValidateHideZoneIds(
        HideZone[] hideZones
    )
    {
        HashSet<string> usedIds =
            new HashSet<string>();

        foreach (HideZone zone in hideZones)
        {
            if (
                string.IsNullOrWhiteSpace(
                    zone.ZoneId
                )
            )
            {
                Debug.LogError(
                    $"{zone.gameObject.name}의 " +
                    "Zone Id가 비어 있습니다."
                );

                return false;
            }

            if (!usedIds.Add(zone.ZoneId))
            {
                Debug.LogError(
                    "중복된 HideZone ID가 있습니다.\n" +
                    $"중복 ID: {zone.ZoneId}"
                );

                return false;
            }
        }

        return true;
    }

    [ContextMenu("AI 숨김 프롬프트 출력 테스트")]
    private void PrintPromptForTest()
    {
        string prompt = BuildPrompt();

        if (string.IsNullOrWhiteSpace(prompt))
        {
            Debug.LogError(
                "AI 숨김 프롬프트 생성에 실패했습니다."
            );

            return;
        }

        Debug.Log(
            "=== Gemini 숨김 계획 프롬프트 ===\n" +
            prompt
        );
    }
}