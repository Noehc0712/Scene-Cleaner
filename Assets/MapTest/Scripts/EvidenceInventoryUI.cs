using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class EvidenceInventoryUI : MonoBehaviour
{
    private const int RequiredSlotCount = 4;

    [SerializeField] private Image[] evidenceSlots = new Image[RequiredSlotCount];

    private static EvidenceInventoryUI instance;
    private int collectedCount;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogError("EvidenceInventoryUI는 씬에 하나만 있어야 합니다.", this);
            enabled = false;
            return;
        }

        if (evidenceSlots == null || evidenceSlots.Length != RequiredSlotCount)
        {
            Debug.LogError("증거 인벤토리 슬롯을 정확히 4개 연결해 주세요.", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < evidenceSlots.Length; i++)
        {
            if (evidenceSlots[i] == null)
            {
                Debug.LogError($"Evidence Slot {i + 1}이 연결되지 않았습니다.", this);
                enabled = false;
                return;
            }

            evidenceSlots[i].sprite = null;
            evidenceSlots[i].preserveAspect = true;
            evidenceSlots[i].raycastTarget = false;
            evidenceSlots[i].gameObject.SetActive(false);
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public static bool TryCollect(Sprite evidenceSprite)
    {
        if (instance == null || !instance.enabled)
        {
            Debug.LogWarning("EvidenceInventoryUI가 씬에 없거나 설정되지 않았습니다.");
            return false;
        }

        return instance.Collect(evidenceSprite);
    }

    private bool Collect(Sprite evidenceSprite)
    {
        if (evidenceSprite == null)
        {
            Debug.LogWarning("범죄 흔적의 Result Image가 비어 있어 인벤토리에 추가할 수 없습니다.");
            return false;
        }

        if (collectedCount >= evidenceSlots.Length)
        {
            Debug.LogWarning("증거 인벤토리가 가득 찼습니다.");
            return false;
        }

        Image targetSlot = evidenceSlots[collectedCount];
        targetSlot.sprite = evidenceSprite;
        targetSlot.gameObject.SetActive(true);
        collectedCount++;
        return true;
    }
}
