using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class EvidenceInventoryUI : MonoBehaviour
{
    private const int RequiredSlotCount = 4;

    [SerializeField]
    private Image[] evidenceSlots =
        new Image[RequiredSlotCount];

    private static EvidenceInventoryUI instance;

    private int collectedCount;


    public static int CollectedCount =>
        instance != null
            ? instance.collectedCount
            : 0;


    private void Awake()
    {
        if (instance != null &&
            instance != this)
        {
            Debug.LogError(
                "EvidenceInventoryUI는 씬에 하나만 있어야 합니다.",
                this
            );

            enabled = false;
            return;
        }


        if (evidenceSlots == null ||
            evidenceSlots.Length != RequiredSlotCount)
        {
            Debug.LogError(
                "증거 인벤토리 슬롯을 정확히 4개 연결해 주세요.",
                this
            );

            enabled = false;
            return;
        }


        for (int i = 0;
             i < evidenceSlots.Length;
             i++)
        {
            if (evidenceSlots[i] == null)
            {
                Debug.LogError(
                    $"Evidence Slot {i + 1}이 연결되지 않았습니다.",
                    this
                );

                enabled = false;
                return;
            }
        }


        instance = this;


        /*
         * 게임 시작 시에도
         * 같은 Reset 함수를 사용한다.
         */
        ResetInventoryInternal();
    }


    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }


    // =============================================================
    // 증거 수집
    // =============================================================

    public static bool TryCollect(
        Sprite evidenceSprite
    )
    {
        if (instance == null ||
            !instance.enabled)
        {
            Debug.LogWarning(
                "EvidenceInventoryUI가 씬에 없거나 설정되지 않았습니다."
            );

            return false;
        }


        return instance.Collect(
            evidenceSprite
        );
    }


    private bool Collect(
        Sprite evidenceSprite
    )
    {
        if (evidenceSprite == null)
        {
            Debug.LogWarning(
                "범죄 흔적의 Result Image가 비어 있어 " +
                "인벤토리에 추가할 수 없습니다."
            );

            return false;
        }


        if (collectedCount >=
            evidenceSlots.Length)
        {
            Debug.LogWarning(
                "증거 인벤토리가 가득 찼습니다."
            );

            return false;
        }


        Image targetSlot =
            evidenceSlots[collectedCount];


        targetSlot.sprite =
            evidenceSprite;

        targetSlot.preserveAspect =
            true;

        targetSlot.raycastTarget =
            false;

        targetSlot.gameObject.SetActive(
            true
        );


        collectedCount++;


        return true;
    }


    // =============================================================
    // 라운드 Reset
    // =============================================================

    /// <summary>
    /// 새로운 라운드 시작 전에
    /// 인벤토리에 모아둔 단서 이미지를 모두 제거한다.
    /// </summary>
    public static void ResetInventory()
    {
        if (instance == null ||
            !instance.enabled)
        {
            Debug.LogWarning(
                "증거 인벤토리 초기화 실패: " +
                "EvidenceInventoryUI가 없습니다."
            );

            return;
        }


        instance.ResetInventoryInternal();
    }


    private void ResetInventoryInternal()
    {
        collectedCount = 0;


        for (int i = 0;
             i < evidenceSlots.Length;
             i++)
        {
            Image slot =
                evidenceSlots[i];


            if (slot == null)
            {
                continue;
            }


            slot.sprite =
                null;

            slot.preserveAspect =
                true;

            slot.raycastTarget =
                false;

            slot.gameObject.SetActive(
                false
            );
        }


        Debug.Log(
            "=== 증거 인벤토리 초기화 완료 ===\n" +
            $"초기화된 슬롯 수: {evidenceSlots.Length}\n" +
            $"현재 수집 단서 수: {collectedCount}"
        );
    }


    // =============================================================
    // 개발 단계 테스트용
    // =============================================================

    [ContextMenu("증거 인벤토리 초기화")]
    private void ResetInventoryForTest()
    {
        ResetInventory();
    }
}