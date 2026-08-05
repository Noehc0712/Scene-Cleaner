using UnityEngine;

public enum EvidenceType
{
    Clue,
    Decoy
}

public enum EvidencePlacementType
{
    PhysicalObject,
    SurfaceTrace
}

public class EvidenceObject : MonoBehaviour
{
    [Header("증거물 기본 정보")]

    [SerializeField]
    private string evidenceId = "bloody_knife_01";

    [SerializeField]
    private string evidenceName = "피 묻은 칼";

    [SerializeField]
    private EvidenceType evidenceType = EvidenceType.Clue;

    [SerializeField]
    private EvidencePlacementType placementType =
        EvidencePlacementType.PhysicalObject;

    [SerializeField]
    [TextArea(2, 4)]
    private string evidenceDescription =
        "프로토타입에서는 Cube로 표현되는 증거물입니다.";

    [Header("현재 상태")]

    [SerializeField]
    private bool isHiddenByAI;

    [SerializeField]
    private bool isFoundByPlayer;

    [SerializeField]
    private string assignedHideZoneId = "";

    [Header("수동 테스트용")]

    [SerializeField]
    private HideZone testHideZone;

    private HideZone assignedHideZone;

    public string EvidenceId
    {
        get
        {
            return evidenceId;
        }
    }

    public string EvidenceName
    {
        get
        {
            return evidenceName;
        }
    }

    public EvidenceType EvidenceType
    {
        get
        {
            return evidenceType;
        }
    }

    public EvidencePlacementType PlacementType
    {
        get
        {
            return placementType;
        }
    }

    public string EvidenceDescription
    {
        get
        {
            return evidenceDescription;
        }
    }

    public bool IsHiddenByAI
    {
        get
        {
            return isHiddenByAI;
        }
    }

    public bool IsFoundByPlayer
    {
        get
        {
            return isFoundByPlayer;
        }
    }

    public string AssignedHideZoneId
    {
        get
        {
            return assignedHideZoneId;
        }
    }

    /*
     * 기존 EvidenceStateCollector와의 호환을 위해
     * 임시로 유지하는 속성이다.
     */
    public bool IsHidden
    {
        get
        {
            return isHiddenByAI;
        }
    }

    /// <summary>
    /// 증거물을 지정한 HideZone의 HidePoint로 이동시키고
    /// AI가 숨긴 상태로 변경한다.
    ///
    /// 현재 테스트 단계에서는 SurfaceTrace도 Cube로 이동시킨다.
    /// 이후에는 흔적형 배치 기능을 별도로 연결할 예정이다.
    /// </summary>
    public bool HideAt(HideZone zone)
    {
        if (zone == null)
        {
            Debug.LogError(
                $"{evidenceName}을 숨길 HideZone이 없습니다."
            );

            return false;
        }

        if (zone.HidePoint == null)
        {
            Debug.LogError(
                $"{zone.ZoneName}에 HidePoint가 연결되지 않았습니다."
            );

            return false;
        }
        if (!zone.SupportsPlacementType(placementType))
        {
            Debug.LogError(
                "=== 배치 형태 불일치 ===\n" +
                $"증거물: {evidenceName}\n" +
                $"증거물 배치 형태: {placementType}\n" +
                $"선택된 장소: {zone.ZoneName}\n" +
                "이 장소에는 해당 형태를 배치할 수 없습니다."
        );

        return false;
    }

        if (assignedHideZone == zone && isHiddenByAI)
        {
            Debug.LogWarning(
                $"{evidenceName}은 이미 {zone.ZoneName}에 숨겨져 있습니다."
            );

            return false;
        }

        if (!zone.TryReserve())
        {
            Debug.LogWarning(
                $"{evidenceName}을 {zone.ZoneName}에 숨길 수 없습니다."
            );

            return false;
        }

        if (assignedHideZone != null &&
            assignedHideZone != zone)
        {
            assignedHideZone.Release();
        }

        assignedHideZone = zone;
        assignedHideZoneId = zone.ZoneId;

        transform.SetPositionAndRotation(
            zone.HidePoint.position,
            zone.HidePoint.rotation
        );

        isHiddenByAI = true;
        isFoundByPlayer = false;

        Debug.Log(
            "=== AI 증거물 숨김 테스트 ===\n" +
            $"증거물 ID: {evidenceId}\n" +
            $"증거물 이름: {evidenceName}\n" +
            $"물건 종류: {evidenceType}\n" +
            $"배치 형태: {placementType}\n" +
            $"숨김 장소: {zone.ZoneName}\n" +
            $"숨김 장소 ID: {zone.ZoneId}\n" +
            $"이동 위치: {zone.HidePoint.position}"
        );

        return true;
    }

    /// <summary>
    /// 플레이어가 해당 물건 또는 흔적을 발견한 상태로 변경한다.
    /// </summary>
    public void MarkAsFound()
    {
        if (isFoundByPlayer)
        {
            Debug.LogWarning(
                $"{evidenceName}은 이미 발견된 항목입니다."
            );

            return;
        }

        isFoundByPlayer = true;
        isHiddenByAI = false;

        if (assignedHideZone != null)
        {
            assignedHideZone.Release();
            assignedHideZone = null;
        }

        assignedHideZoneId = "";

        Debug.Log(
            "=== 플레이어 증거 발견 ===\n" +
            $"이름: {evidenceName}\n" +
            $"종류: {evidenceType}\n" +
            $"배치 형태: {placementType}"
        );
    }

    /// <summary>
    /// 증거물의 숨김 및 발견 상태를 초기화한다.
    /// </summary>
    public void ResetEvidence()
    {
        if (assignedHideZone != null)
        {
            assignedHideZone.Release();
            assignedHideZone = null;
        }

        assignedHideZoneId = "";
        isHiddenByAI = false;
        isFoundByPlayer = false;

        Debug.Log(
            $"{evidenceName} 상태를 초기화했습니다."
        );
    }

    [ContextMenu("증거물 정보 확인")]
    private void PrintEvidenceInfoForTest()
    {
        Debug.Log(
            "=== 증거물 정보 ===\n" +
            $"ID: {evidenceId}\n" +
            $"이름: {evidenceName}\n" +
            $"정답 구분: {evidenceType}\n" +
            $"배치 형태: {placementType}\n" +
            $"설명: {evidenceDescription}"
        );
    }

    [ContextMenu("테스트 HideZone에 숨기기")]
    private void HideAtTestZone()
    {
        HideAt(testHideZone);
    }

    [ContextMenu("플레이어 발견 테스트")]
    private void MarkAsFoundForTest()
    {
        MarkAsFound();
    }

    [ContextMenu("증거물 상태 초기화")]
    private void ResetEvidenceForTest()
    {
        ResetEvidence();
    }
}