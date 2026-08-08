using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class CCTVInteractable : MonoBehaviour
{
    [Flags]
    private enum CameraAccess
    {
        None = 0,
        CCTV01 = 1 << 0,
        CCTV02 = 1 << 1,
        CCTV03 = 1 << 2,
        CCTV04 = 1 << 3,
        All = CCTV01 | CCTV02 | CCTV03 | CCTV04
    }

    [Header("Investigation")]
    [SerializeField] private string displayName = "수상한 물건";
    [SerializeField] private bool isCrimeEvidence;
    [SerializeField, TextArea(2, 5)] private string resultDescription;
    [SerializeField] private Sprite resultImage;

    [Header("Camera Access")]
    [SerializeField] private CameraAccess allowedCameras = CameraAccess.All;

    [Header("Events")]
    [SerializeField] private UnityEvent onClicked;

    public string DisplayName => displayName;
    public bool IsCrimeEvidence => isCrimeEvidence;
    public string ResultDescription => resultDescription;
    public Sprite ResultImage => resultImage;
    public bool IsInvestigated { get; private set; }

    public bool CanInteractFromCamera(int cameraNumber)
    {
        if (cameraNumber is < 1 or > 4)
            return false;

        CameraAccess activeCamera = (CameraAccess)(1 << (cameraNumber - 1));
        return (allowedCameras & activeCamera) != 0;
    }

    public void Interact()
    {
        if (!CanInteractFromCamera(CCTVSwitcher.CurrentCameraNumber))
            return;

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
