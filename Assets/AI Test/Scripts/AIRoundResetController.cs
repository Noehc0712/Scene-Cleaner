using UnityEngine;

public class AIRoundResetController : MonoBehaviour
{
    /// <summary>
    /// 다음 라운드의 AI 재배치를 위해
    /// EvidenceObject, HideZone, CCTVInteractable 상태를 초기화한다.
    ///
    /// 플레이어의 1라운드 행동 기록은 삭제하지 않는다.
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
        // 4. 증거물 상태 초기화
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
        // 7. 결과 로그
        // =========================================================

        Debug.Log(
            "=== AI 라운드 상태 초기화 완료 ===\n" +
            $"초기화된 증거물 수: {evidenceObjects.Length}\n" +
            $"초기화된 HideZone 수: {hideZones.Length}\n" +
            $"초기화된 조사 장소 수: {interactables.Length}\n" +
            "플레이어 행동 기록은 유지됩니다.\n" +
            "다음 AI 숨김 계획을 적용할 수 있습니다."
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