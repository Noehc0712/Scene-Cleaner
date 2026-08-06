using UnityEngine;

public class AIRoundResetController : MonoBehaviour
{
    /// <summary>
    /// 다음 라운드를 시작하기 전에
    /// 모든 증거물과 HideZone 상태를 초기화한다.
    /// </summary>
    public void ResetRoundState()
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
         * 각 증거물이 사용하던 HideZone을 해제하고
         * 숨김 및 발견 상태를 초기화한다.
         */
        foreach (EvidenceObject evidence in evidenceObjects)
        {
            if (evidence == null)
            {
                continue;
            }

            evidence.ResetEvidence();
        }

        /*
         * 증거물과 연결이 끊겼는데도 예약 상태가 남아 있는
         * HideZone이 있을 수 있으므로 한 번 더 초기화한다.
         */
        foreach (HideZone zone in hideZones)
        {
            if (zone == null)
            {
                continue;
            }

            zone.Release();
        }

        Debug.Log(
            "=== AI 라운드 상태 초기화 완료 ===\n" +
            $"초기화된 증거물 수: {evidenceObjects.Length}\n" +
            $"초기화된 HideZone 수: {hideZones.Length}\n" +
            "다음 AI 숨김 계획을 적용할 수 있습니다."
        );
    }

    [ContextMenu("AI 라운드 상태 초기화")]
    private void ResetRoundStateForTest()
    {
        ResetRoundState();
    }
}