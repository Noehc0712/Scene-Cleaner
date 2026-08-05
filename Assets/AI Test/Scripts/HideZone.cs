using UnityEngine;

public enum HideDifficulty
{
    Easy,
    Normal,
    Hard
}

public class HideZone : MonoBehaviour
{
    [Header("숨김 장소 기본 정보")]

    [SerializeField]
    private string zoneId = "hide_zone_01";

    [SerializeField]
    private string zoneName = "숨김 장소";

    [SerializeField]
    private HideDifficulty difficulty = HideDifficulty.Easy;

    [Header("배치 가능한 형태")]

    [SerializeField]
    private bool allowPhysicalObject = true;

    [SerializeField]
    private bool allowSurfaceTrace = false;

    [Header("실제 배치 위치")]

    [SerializeField]
    private Transform hidePoint;

    [Header("현재 상태")]

    [SerializeField]
    private bool isOccupied;

    public string ZoneId
    {
        get
        {
            return zoneId;
        }
    }

    public string ZoneName
    {
        get
        {
            return zoneName;
        }
    }

    public HideDifficulty Difficulty
    {
        get
        {
            return difficulty;
        }
    }

    public Transform HidePoint
    {
        get
        {
            return hidePoint;
        }
    }

    public bool IsOccupied
    {
        get
        {
            return isOccupied;
        }
    }

    public bool AllowPhysicalObject
    {
        get
        {
            return allowPhysicalObject;
        }
    }

    public bool AllowSurfaceTrace
    {
        get
        {
            return allowSurfaceTrace;
        }
    }

    /// <summary>
    /// 전달받은 배치 형태를 이 HideZone이 지원하는지 확인한다.
    /// </summary>
    public bool SupportsPlacementType(
        EvidencePlacementType placementType
    )
    {
        switch (placementType)
        {
            case EvidencePlacementType.PhysicalObject:
                return allowPhysicalObject;

            case EvidencePlacementType.SurfaceTrace:
                return allowSurfaceTrace;

            default:
                return false;
        }
    }

    /// <summary>
    /// 현재 장소를 증거물 배치용으로 예약한다.
    /// 이미 사용 중이면 예약하지 않는다.
    /// </summary>
    public bool TryReserve()
    {
        if (isOccupied)
        {
            Debug.LogWarning(
                $"{zoneName}은 이미 사용 중인 숨김 장소입니다."
            );

            return false;
        }

        isOccupied = true;
        return true;
    }

    /// <summary>
    /// 현재 장소의 사용 상태를 해제한다.
    /// </summary>
    public void Release()
    {
        isOccupied = false;
    }

    [ContextMenu("숨김 장소 설정 확인")]
    private void PrintZoneInformation()
    {
        string hidePointName =
            hidePoint != null
                ? hidePoint.name
                : "연결되지 않음";

        Debug.Log(
            "=== 숨김 장소 설정 ===\n" +
            $"장소 ID: {zoneId}\n" +
            $"장소 이름: {zoneName}\n" +
            $"난이도: {difficulty}\n" +
            $"물건형 허용: {allowPhysicalObject}\n" +
            $"흔적형 허용: {allowSurfaceTrace}\n" +
            $"HidePoint: {hidePointName}\n" +
            $"현재 사용 중: {isOccupied}"
        );
    }

    private void OnValidate()
    {
        if (hidePoint == null)
        {
            Transform childHidePoint =
                transform.Find("HidePoint");

            if (childHidePoint != null)
            {
                hidePoint = childHidePoint;
            }
        }
    }
}