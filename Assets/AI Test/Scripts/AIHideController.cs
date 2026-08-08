using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class AIHideController : MonoBehaviour
{
    [Header("수동 배치 테스트")]

    [SerializeField]
    private EvidenceObject testEvidence;

    [SerializeField]
    private string testHideZoneId = "hide_zone_01";

    /// <summary>
    /// Gemini가 생성한 숨김 계획을 검사하고 적용한다.
    ///
    /// 모든 계획을 먼저 검사하며,
    /// 하나라도 잘못되어 있으면 어떤 물건도 이동시키지 않는다.
    /// </summary>
    public bool ApplyHidePlan(AIHidePlan plan)
    {
        EvidenceObject[] evidenceObjects =
            FindObjectsByType<EvidenceObject>(
                FindObjectsSortMode.None
            );

        HideZone[] hideZones =
            FindObjectsByType<HideZone>(
                FindObjectsSortMode.None
            );

        Dictionary<string, EvidenceObject> evidenceById =
            CreateEvidenceDictionary(evidenceObjects);

        Dictionary<string, HideZone> hideZoneById =
            CreateHideZoneDictionary(hideZones);

        if (evidenceById == null || hideZoneById == null)
        {
            return false;
        }

        /*
         * 실제 오브젝트를 이동하기 전에
         * Gemini의 계획 전체를 먼저 검사한다.
         */
        if (!ValidateEntirePlan(
                plan,
                evidenceById,
                hideZoneById
            ))
        {
            Debug.LogError(
                "Gemini 숨김 계획 검증에 실패했습니다.\n" +
                "어떤 증거물도 이동하지 않았습니다."
            );

            return false;
        }

        /*
         * 전체 검증을 통과한 뒤에만
         * 실제 배치를 시작한다.
         */


        // =========================================================
        // 1. 모든 HideZone의 조사 결과를 먼저 Empty 상태로 초기화
        // =========================================================

        foreach (HideZone zone in hideZones)
        {
            CCTVInteractable interactable =
                zone.GetComponent<CCTVInteractable>();

            if (interactable != null)
            {
                interactable.SetInvestigationResult(null);

                Debug.Log(
                    "[조사 장소 초기화 성공]\n" +
                    $"Zone Name: {zone.ZoneName}\n" +
                    $"Zone ID: {zone.ZoneId}\n" +
                    $"GameObject: {zone.gameObject.name}"
                );
            }
            else
            {
                Debug.LogWarning(
                    "[조사 장소 초기화 실패 - CCTVInteractable 없음]\n" +
                    $"Zone Name: {zone.ZoneName}\n" +
                    $"Zone ID: {zone.ZoneId}\n" +
                    $"GameObject: {zone.gameObject.name}"
                );
            }
        }


        // =========================================================
        // 2. Gemini가 선택한 6개 장소에 증거물 배치
        // =========================================================

        StringBuilder resultLog = new StringBuilder();

        resultLog.AppendLine(
            "=== Gemini 숨김 계획 적용 결과 ==="
        );

        foreach (AIHideAssignment assignment in plan.assignments)
        {
            EvidenceObject evidence =
                evidenceById[assignment.evidenceId];

            HideZone zone =
                hideZoneById[assignment.hideZoneId];

            bool wasHidden = evidence.HideAt(zone);

            if (!wasHidden)
            {
                Debug.LogError(
                    "검증은 통과했지만 실제 배치 중 오류가 발생했습니다.\n" +
                    $"증거물 ID: {assignment.evidenceId}\n" +
                    $"장소 ID: {assignment.hideZoneId}"
                );

                return false;
            }


            // =====================================================
            // 3. 해당 HideZone의 CCTVInteractable에
            //    EvidenceObject의 조사 결과 자동 입력
            // =====================================================

            CCTVInteractable interactable =
                zone.GetComponent<CCTVInteractable>();

            if (interactable != null)
            {
                interactable.SetInvestigationResult(evidence);

                string imageName =
                    interactable.ResultImage != null
                        ? interactable.ResultImage.name
                        : "None";

                string descriptionState =
                    string.IsNullOrWhiteSpace(
                        interactable.ResultDescription
                    )
                        ? "비어 있음"
                        : "정상";

                Debug.Log(
                    "=== 조사 결과 자동 연결 ===\n" +
                    $"장소: {zone.ZoneName}\n" +
                    $"장소 ID: {zone.ZoneId}\n" +
                    $"증거물: {evidence.EvidenceName}\n" +
                    $"증거물 ID: {evidence.EvidenceId}\n" +
                    $"종류: {evidence.EvidenceType}\n" +
                    $"Is Crime Evidence: {interactable.IsCrimeEvidence}\n" +
                    $"설명 상태: {descriptionState}\n" +
                    $"Result Image: {imageName}"
                );

                /*
                 * 증거물이 배치됐는데
                 * 설명이나 이미지가 없는 경우 바로 경고한다.
                 */
                if (string.IsNullOrWhiteSpace(
                        interactable.ResultDescription
                    ))
                {
                    Debug.LogWarning(
                        "=== 조사 결과 연결 이상 ===\n" +
                        $"장소: {zone.ZoneName}\n" +
                        $"증거물: {evidence.EvidenceName}\n" +
                        "Result Description이 비어 있습니다."
                    );
                }

                if (interactable.ResultImage == null)
                {
                    Debug.LogWarning(
                        "=== 조사 결과 연결 이상 ===\n" +
                        $"장소: {zone.ZoneName}\n" +
                        $"증거물: {evidence.EvidenceName}\n" +
                        "Result Image가 비어 있습니다."
                    );
                }
            }
            else
            {
                Debug.LogWarning(
                    "=== 조사 결과 연결 실패 ===\n" +
                    $"장소: {zone.ZoneName}\n" +
                    $"장소 ID: {zone.ZoneId}\n" +
                    "CCTVInteractable이 없습니다."
                );
            }


            // Gemini 결과 로그 작성
            resultLog.AppendLine(
                $"- {evidence.EvidenceName}"
            );

            resultLog.AppendLine(
                $"  선택 장소: {zone.ZoneName}"
            );

            resultLog.AppendLine(
                $"  장소 ID: {zone.ZoneId}"
            );
        }

        Debug.Log(resultLog.ToString());


        // =========================================================
        // 4. 배치가 전부 끝난 후 10개 장소 최종 검증
        // =========================================================

        VerifyInvestigationResults(
            hideZones,
            evidenceObjects
        );

        return true;
    }


    /// <summary>
    /// AI 배치가 모두 끝난 뒤
    /// 10개의 HideZone과 CCTVInteractable 상태를 검사한다.
    ///
    /// 증거물이 들어간 6곳:
    /// - EvidenceType
    /// - Description
    /// - ResultImage
    /// 가 EvidenceObject와 일치해야 한다.
    ///
    /// 증거물이 없는 4곳:
    /// - IsCrimeEvidence = false
    /// - ResultImage = null
    /// - 빈 장소 설명 존재
    /// 상태여야 한다.
    /// </summary>
    private void VerifyInvestigationResults(
        HideZone[] hideZones,
        EvidenceObject[] evidenceObjects
    )
    {
        Dictionary<string, EvidenceObject> evidenceByZoneId =
            new Dictionary<string, EvidenceObject>();


        /*
         * EvidenceObject가 현재 어느 HideZone에
         * 배치되어 있는지 Zone ID 기준으로 정리한다.
         */
        foreach (EvidenceObject evidence in evidenceObjects)
        {
            if (evidence == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(
                    evidence.AssignedHideZoneId
                ))
            {
                continue;
            }

            evidenceByZoneId[
                evidence.AssignedHideZoneId
            ] = evidence;
        }


        StringBuilder verificationLog =
            new StringBuilder();

        verificationLog.AppendLine(
            "=== CCTV 조사 결과 최종 검증 ==="
        );

        int normalCount = 0;
        int problemCount = 0;


        foreach (HideZone zone in hideZones)
        {
            CCTVInteractable interactable =
                zone.GetComponent<CCTVInteractable>();


            // CCTVInteractable 자체가 없는 경우
            if (interactable == null)
            {
                problemCount++;

                verificationLog.AppendLine(
                    $"[문제] {zone.ZoneName}"
                );

                verificationLog.AppendLine(
                    "  CCTVInteractable 없음"
                );

                continue;
            }


            /*
             * 이 HideZone에 EvidenceObject가 있는 경우
             */
            if (evidenceByZoneId.TryGetValue(
                    zone.ZoneId,
                    out EvidenceObject evidence
                ))
            {
                bool expectedCrimeEvidence =
                    evidence.EvidenceType ==
                    EvidenceType.Clue;

                bool crimeStateMatches =
                    interactable.IsCrimeEvidence ==
                    expectedCrimeEvidence;

                bool descriptionMatches =
                    interactable.ResultDescription ==
                    evidence.EvidenceDescription;

                bool imageMatches =
                    interactable.ResultImage ==
                    evidence.ResultImage;

                bool resultImageExists =
                    interactable.ResultImage != null;


                if (crimeStateMatches &&
                    descriptionMatches &&
                    imageMatches &&
                    resultImageExists)
                {
                    normalCount++;

                    verificationLog.AppendLine(
                        $"[정상] {zone.ZoneName}"
                    );

                    verificationLog.AppendLine(
                        $"  증거물: {evidence.EvidenceName}"
                    );

                    verificationLog.AppendLine(
                        $"  이미지: {interactable.ResultImage.name}"
                    );
                }
                else
                {
                    problemCount++;

                    verificationLog.AppendLine(
                        $"[문제] {zone.ZoneName}"
                    );

                    verificationLog.AppendLine(
                        $"  증거물: {evidence.EvidenceName}"
                    );

                    verificationLog.AppendLine(
                        $"  IsCrimeEvidence 일치: {crimeStateMatches}"
                    );

                    verificationLog.AppendLine(
                        $"  설명 일치: {descriptionMatches}"
                    );

                    verificationLog.AppendLine(
                        $"  이미지 일치: {imageMatches}"
                    );

                    verificationLog.AppendLine(
                        $"  이미지 존재: {resultImageExists}"
                    );

                    verificationLog.AppendLine(
                        $"  현재 이미지: " +
                        $"{(interactable.ResultImage != null ? interactable.ResultImage.name : "None")}"
                    );
                }
            }

            /*
             * 이 HideZone에 아무 EvidenceObject도 없는 경우
             */
            else
            {
                bool crimeStateIsEmpty =
                    !interactable.IsCrimeEvidence;

                bool imageIsEmpty =
                    interactable.ResultImage == null;

                bool descriptionExists =
                    !string.IsNullOrWhiteSpace(
                        interactable.ResultDescription
                    );


                if (crimeStateIsEmpty &&
                    imageIsEmpty &&
                    descriptionExists)
                {
                    normalCount++;

                    verificationLog.AppendLine(
                        $"[정상 - 빈 장소] {zone.ZoneName}"
                    );
                }
                else
                {
                    problemCount++;

                    verificationLog.AppendLine(
                        $"[문제 - 빈 장소] {zone.ZoneName}"
                    );

                    verificationLog.AppendLine(
                        $"  IsCrimeEvidence: {interactable.IsCrimeEvidence}"
                    );

                    verificationLog.AppendLine(
                        $"  Result Image: " +
                        $"{(interactable.ResultImage != null ? interactable.ResultImage.name : "None")}"
                    );

                    verificationLog.AppendLine(
                        $"  설명 존재: {descriptionExists}"
                    );
                }
            }
        }


        verificationLog.AppendLine(
            "------------------------------"
        );

        verificationLog.AppendLine(
            $"정상 장소 수: {normalCount}"
        );

        verificationLog.AppendLine(
            $"문제 장소 수: {problemCount}"
        );


        Debug.Log(
            verificationLog.ToString()
        );


        if (problemCount > 0)
        {
            Debug.LogWarning(
                "CCTV 조사 결과 자동 연결에서 " +
                $"{problemCount}개의 문제를 발견했습니다."
            );
        }
        else
        {
            Debug.Log(
                "=== CCTV 조사 결과 자동 연결 전체 정상 ==="
            );
        }
    }


    /// <summary>
    /// Gemini가 반환한 숨김 계획 전체를 검사한다.
    /// 이 함수에서는 오브젝트를 이동하지 않는다.
    /// </summary>
    private bool ValidateEntirePlan(
        AIHidePlan plan,
        Dictionary<string, EvidenceObject> evidenceById,
        Dictionary<string, HideZone> hideZoneById
    )
    {
        if (plan == null)
        {
            Debug.LogError(
                "Gemini 숨김 계획이 null입니다."
            );

            return false;
        }

        if (plan.assignments == null)
        {
            Debug.LogError(
                "Gemini 응답에 assignments 목록이 없습니다."
            );

            return false;
        }

        if (plan.assignments.Length != evidenceById.Count)
        {
            Debug.LogError(
                "Gemini가 반환한 배치 개수가 올바르지 않습니다.\n" +
                $"씬의 증거물 수: {evidenceById.Count}\n" +
                $"반환된 배치 수: {plan.assignments.Length}"
            );

            return false;
        }


        HashSet<string> usedEvidenceIds =
            new HashSet<string>();

        HashSet<string> usedHideZoneIds =
            new HashSet<string>();


        foreach (AIHideAssignment assignment in plan.assignments)
        {
            if (assignment == null)
            {
                Debug.LogError(
                    "Gemini 계획에 비어 있는 배치 항목이 있습니다."
                );

                return false;
            }

            if (string.IsNullOrWhiteSpace(
                    assignment.evidenceId
                ))
            {
                Debug.LogError(
                    "Evidence ID가 비어 있는 배치 항목이 있습니다."
                );

                return false;
            }

            if (string.IsNullOrWhiteSpace(
                    assignment.hideZoneId
                ))
            {
                Debug.LogError(
                    "HideZone ID가 비어 있는 배치 항목이 있습니다."
                );

                return false;
            }


            /*
             * 같은 증거물이 두 번 배치되는지 검사한다.
             */
            if (!usedEvidenceIds.Add(
                    assignment.evidenceId
                ))
            {
                Debug.LogError(
                    "같은 증거물이 두 번 이상 배치되었습니다.\n" +
                    $"중복 Evidence ID: {assignment.evidenceId}"
                );

                return false;
            }


            /*
             * 같은 HideZone을 두 번 사용하는지 검사한다.
             */
            if (!usedHideZoneIds.Add(
                    assignment.hideZoneId
                ))
            {
                Debug.LogError(
                    "같은 HideZone이 두 번 이상 선택되었습니다.\n" +
                    $"중복 HideZone ID: {assignment.hideZoneId}"
                );

                return false;
            }


            /*
             * 실제 씬에 존재하는 Evidence ID인지 확인한다.
             */
            if (!evidenceById.TryGetValue(
                    assignment.evidenceId,
                    out EvidenceObject evidence
                ))
            {
                Debug.LogError(
                    "씬에 존재하지 않는 Evidence ID입니다.\n" +
                    $"잘못된 ID: {assignment.evidenceId}"
                );

                return false;
            }


            /*
             * 실제 씬에 존재하는 HideZone ID인지 확인한다.
             */
            if (!hideZoneById.TryGetValue(
                    assignment.hideZoneId,
                    out HideZone zone
                ))
            {
                Debug.LogError(
                    "씬에 존재하지 않는 HideZone ID입니다.\n" +
                    $"잘못된 ID: {assignment.hideZoneId}"
                );

                return false;
            }


            if (zone.HidePoint == null)
            {
                Debug.LogError(
                    "HidePoint가 연결되지 않은 장소가 선택되었습니다.\n" +
                    $"장소: {zone.ZoneName}\n" +
                    $"장소 ID: {zone.ZoneId}"
                );

                return false;
            }


            if (zone.IsOccupied)
            {
                Debug.LogError(
                    "이미 사용 중인 HideZone이 선택되었습니다.\n" +
                    $"장소: {zone.ZoneName}\n" +
                    $"장소 ID: {zone.ZoneId}"
                );

                return false;
            }


            /*
             * PhysicalObject와 SurfaceTrace 조건을 검사한다.
             */
            if (!zone.SupportsPlacementType(
                    evidence.PlacementType
                ))
            {
                Debug.LogError(
                    "=== 숨김 계획 배치 형태 불일치 ===\n" +
                    $"증거물: {evidence.EvidenceName}\n" +
                    $"증거물 ID: {evidence.EvidenceId}\n" +
                    $"배치 형태: {evidence.PlacementType}\n" +
                    $"선택 장소: {zone.ZoneName}\n" +
                    $"장소 ID: {zone.ZoneId}"
                );

                return false;
            }
        }


        /*
         * 모든 EvidenceObject가 계획에 정확히 포함됐는지 확인한다.
         */
        foreach (string evidenceId in evidenceById.Keys)
        {
            if (!usedEvidenceIds.Contains(evidenceId))
            {
                Debug.LogError(
                    "Gemini 계획에서 누락된 증거물이 있습니다.\n" +
                    $"누락 Evidence ID: {evidenceId}"
                );

                return false;
            }
        }


        Debug.Log(
            "=== Gemini 숨김 계획 전체 검증 성공 ===\n" +
            $"증거물 수: {usedEvidenceIds.Count}\n" +
            $"사용 장소 수: {usedHideZoneIds.Count}\n" +
            "모든 ID, 중복 여부, 배치 형태와 장소 상태가 정상입니다."
        );

        return true;
    }


    /// <summary>
    /// Evidence ID로 EvidenceObject를 찾을 수 있도록
    /// 사전 형태로 정리한다.
    /// </summary>
    private Dictionary<string, EvidenceObject>
        CreateEvidenceDictionary(
            EvidenceObject[] evidenceObjects
        )
    {
        Dictionary<string, EvidenceObject> dictionary =
            new Dictionary<string, EvidenceObject>();


        foreach (EvidenceObject evidence in evidenceObjects)
        {
            if (evidence == null)
            {
                continue;
            }

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

            if (!dictionary.TryAdd(
                    evidence.EvidenceId,
                    evidence
                ))
            {
                Debug.LogError(
                    "씬에 중복된 Evidence ID가 있습니다.\n" +
                    $"중복 ID: {evidence.EvidenceId}"
                );

                return null;
            }
        }

        return dictionary;
    }


    /// <summary>
    /// Zone ID로 HideZone을 찾을 수 있도록
    /// 사전 형태로 정리한다.
    /// </summary>
    private Dictionary<string, HideZone>
        CreateHideZoneDictionary(
            HideZone[] hideZones
        )
    {
        Dictionary<string, HideZone> dictionary =
            new Dictionary<string, HideZone>();


        foreach (HideZone zone in hideZones)
        {
            if (zone == null)
            {
                continue;
            }

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

            if (!dictionary.TryAdd(
                    zone.ZoneId,
                    zone
                ))
            {
                Debug.LogError(
                    "씬에 중복된 HideZone ID가 있습니다.\n" +
                    $"중복 ID: {zone.ZoneId}"
                );

                return null;
            }
        }

        return dictionary;
    }


    /// <summary>
    /// EvidenceObject 하나를 Zone ID로 직접 숨기는
    /// 수동 테스트 기능이다.
    /// </summary>
    public bool HideEvidenceByZoneId(
        EvidenceObject evidence,
        string hideZoneId
    )
    {
        if (evidence == null)
        {
            Debug.LogError(
                "수동 테스트할 EvidenceObject가 없습니다."
            );

            return false;
        }


        HideZone[] hideZones =
            FindObjectsByType<HideZone>(
                FindObjectsSortMode.None
            );


        foreach (HideZone zone in hideZones)
        {
            if (zone.ZoneId == hideZoneId)
            {
                return evidence.HideAt(zone);
            }
        }


        Debug.LogError(
            "입력한 ID와 일치하는 HideZone이 없습니다.\n" +
            $"입력 ID: {hideZoneId}"
        );

        return false;
    }


    [ContextMenu("수동 숨김 테스트")]
    private void RunManualHideTest()
    {
        HideEvidenceByZoneId(
            testEvidence,
            testHideZoneId
        );
    }
}