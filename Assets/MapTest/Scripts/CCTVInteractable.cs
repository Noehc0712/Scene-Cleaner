using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class CCTVInteractable : MonoBehaviour
{
    [Header("Investigation")]
    [SerializeField] private string displayName = "수상한 물건";
    [SerializeField] private bool isCrimeEvidence;
    [SerializeField, TextArea(2, 5)] private string resultDescription;
    [SerializeField] private Sprite resultImage;

    [Header("Events")]
    [SerializeField] private UnityEvent onClicked;

    public string DisplayName => displayName;
    public bool IsCrimeEvidence => isCrimeEvidence;
    public string ResultDescription => resultDescription;
    public Sprite ResultImage => resultImage;
    public bool IsInvestigated { get; private set; }

    public void Interact()
    {
        if (!CCTVInspectionUI.TryOpen(this))
            Debug.LogWarning("CCTV Inspection UI가 씬에 없거나 설정되지 않았습니다.", this);
    }

    public void CompleteInvestigation()
    {
        if (IsInvestigated)
            return;

        IsInvestigated = true;

        if (isCrimeEvidence)
            EvidenceInventoryUI.TryCollect(resultImage);

        onClicked?.Invoke();
    }
}
