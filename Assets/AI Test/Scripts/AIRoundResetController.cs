using UnityEngine;

public class AIRoundResetController : MonoBehaviour
{
    /// <summary>
    /// 다음 라운드 시작을 위해
    /// AI 배치 상태와 플레이어 조사 UI를 초기화한다.
    ///
    /// 주의:
    /// 1라운드 플레이어 행동 기록은 삭제하지 않는다.
    /// 이 데이터는 2라운드 Gemini 적응형 배치에 사용된다.
    /// </summary>
    public void ResetRoundState()
    {
        // =========================================================
        // 1. EvidenceObject 찾기
        // =========================================================

        EvidenceObject[] evidenceObjects =
            FindObjectsByType<EvidenceObject>(
                FindObjectsSortMode.None
            );


        // =========================================================
        // 2. HideZone 찾기
        // =========================================================

        HideZone[] hideZones =
            FindObjectsByType<HideZone>(
                FindObjectsSortMode.None
            );


        // =========================================================
        // 3. CCTVInteractable 찾기
        // =========================================================

        CCTVInteractable[] interactables =
            FindObjectsByType<CCTVInteractable>(
                FindObjectsSortMode.None
            );


        // =========================================================
        // 4. EvidenceObject 상태 초기화
        // =========================================================

        foreach (EvidenceObject evidence in evidenceObjects)
        {
            if (evidence != null)
            {
                evidence.ResetEvidence();
            }
        }


        // =========================================================
        // 5. HideZone 점유 상태 초기화
        // =========================================================

        foreach (HideZone zone in hideZones)
        {
            if (zone != null)
            {
                zone.Release();
            }
        }


        // =========================================================
        // 6. 조사 완료 상태 초기화
        // =========================================================

        foreach (CCTVInteractable interactable in interactables)
        {
            if (interactable != null)
            {
                interactable.ResetInvestigation();
            }
        }


        // =========================================================
        // 7. 탐사 기회 5회 초기화
        // =========================================================

        CCTVInspectionUI.ResetAttempts();


        // =========================================================
        // 8. 단서 인벤토리 초기화
        // =========================================================

        EvidenceInventoryUI.ResetInventory();


        // =========================================================
        // 9. 완료 로그
        // =========================================================

        Debug.Log(
            "=== 라운드 전체 상태 초기화 완료 ===\n" +
            $"초기화된 증거물 수: {evidenceObjects.Length}\n" +
            $"초기화된 HideZone 수: {hideZones.Length}\n" +
            $"초기화된 조사 장소 수: {interactables.Length}\n" +
            "탐사 기회: 5회로 초기화\n" +
            "증거 인벤토리: 초기화 완료\n" +
            "플레이어 행동 기록: 유지\n" +
            "다음 Gemini 숨김 계획을 적용할 수 있습니다."
        );
    }


    // =============================================================
    // 개발 단계 테스트용
    // =============================================================

    [ContextMenu("AI 라운드 상태 초기화")]
    private void ResetRoundStateForTest()
    {
        ResetRoundState();
    }
}