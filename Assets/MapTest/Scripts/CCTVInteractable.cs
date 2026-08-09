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

    [SerializeField]
    private string displayName = "수상한 물건";

    [SerializeField]
    private bool isCrimeEvidence;

    [SerializeField, TextArea(2, 5)]
    private string resultDescription;

    [SerializeField]
    private Sprite resultImage;


    [Header("Camera Access")]

    [SerializeField]
    private CameraAccess allowedCameras = CameraAccess.All;


    [Header("Events")]

    [SerializeField]
    private UnityEvent onClicked;


    public string DisplayName => displayName;

    public bool IsCrimeEvidence => isCrimeEvidence;

    public string ResultDescription => resultDescription;

    public Sprite ResultImage => resultImage;

    public bool IsInvestigated
    {
        get;
        private set;
    }


    public bool CanInteractFromCamera(int cameraNumber)
    {
        if (cameraNumber is < 1 or > 4)
        {
            return false;
        }

        CameraAccess activeCamera =
            (CameraAccess)(1 << (cameraNumber - 1));

        return (allowedCameras & activeCamera) != 0;
    }


    /// <summary>
    /// 플레이어가 이 장소를 클릭했을 때
    /// 조사 확인 UI를 연다.
    /// </summary>
    public void Interact()
    {
        if (!CanInteractFromCamera(
                CCTVSwitcher.CurrentCameraNumber
            ))
        {
            return;
        }

        if (!CCTVInspectionUI.TryOpen(this))
        {
            Debug.LogWarning(
                "CCTV Inspection UI가 씬에 없거나 " +
                "설정되지 않았습니다.",
                this
            );
        }
    }


    /// <summary>
    /// 플레이어가 조사 확인 버튼을 눌러
    /// 실제 조사를 완료했을 때 호출된다.
    /// </summary>
    public void CompleteInvestigation()
    {
        /*
         * 이미 조사한 장소라면
         * 다시 처리하지 않는다.
         */
        if (IsInvestigated)
        {
            return;
        }


        IsInvestigated = true;


        // =========================================================
        // 조사 결과 종류 판단
        // =========================================================

        InvestigationResultType resultType;


        /*
         * 이미지가 없다면
         * AI가 아무것도 배치하지 않은 빈 장소이다.
         */
        if (resultImage == null)
        {
            resultType =
                InvestigationResultType.Empty;
        }

        /*
         * 이미지가 있고 실제 범죄 단서라면 Clue.
         */
        else if (isCrimeEvidence)
        {
            resultType =
                InvestigationResultType.Clue;
        }

        /*
         * 이미지가 있지만 범죄 단서가 아니라면 Decoy.
         */
        else
        {
            resultType =
                InvestigationResultType.Decoy;
        }


        // =========================================================
        // 플레이어 조사 행동 기록
        // =========================================================

        HideZone hideZone =
            GetComponent<HideZone>();


        if (hideZone == null)
        {
            Debug.LogWarning(
                "플레이어 조사 기록 실패:\n" +
                $"{gameObject.name}에 HideZone이 없습니다.",
                this
            );
        }
        else
        {
            PlayerActionRecorder recorder =
                FindFirstObjectByType<PlayerActionRecorder>();


            if (recorder == null)
            {
                Debug.LogWarning(
                    "플레이어 조사 기록 실패:\n" +
                    "씬에 PlayerActionRecorder가 없습니다.",
                    this
                );
            }
            else
            {
                recorder.RecordInvestigation(
                    hideZone,
                    resultType
                );
            }
        }


        // =========================================================
        // 실제 단서라면 인벤토리에 추가
        // =========================================================

        if (isCrimeEvidence)
        {
            EvidenceInventoryUI.TryCollect(
                resultImage
            );
        }


        onClicked?.Invoke();
    }


    /// <summary>
    /// Gemini가 HideZone에 배치한 EvidenceObject의
    /// 조사 결과를 이 CCTVInteractable에 연결한다.
    ///
    /// evidence가 null이면
    /// 아무 증거물도 없는 빈 조사 장소로 설정한다.
    /// </summary>
    public void SetInvestigationResult(
        EvidenceObject evidence
    )
    {
        if (evidence == null)
        {
            isCrimeEvidence = false;

            resultDescription =
                "특별한 흔적을 발견하지 못했습니다.";

            resultImage = null;

            return;
        }


        isCrimeEvidence =
            evidence.EvidenceType ==
            EvidenceType.Clue;


        resultDescription =
            evidence.EvidenceDescription;


        resultImage =
            evidence.ResultImage;
    }


    /// <summary>
    /// 새로운 라운드가 시작되기 전에
    /// 이 장소의 조사 완료 상태를 초기화한다.
    ///
    /// 1라운드에서 이미 조사했던 장소도
    /// 2라운드에서는 다시 조사할 수 있게 된다.
    ///
    /// 여기서는 조사 결과 이미지나 설명은 건드리지 않는다.
    /// 그것들은 AIHideController가 새로운 AI 배치를
    /// 적용하면서 다시 설정한다.
    /// </summary>
    public void ResetInvestigation()
    {
        IsInvestigated = false;
    }
}
