using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class CCTVInteractable : MonoBehaviour
{
    [Header("Investigation")]
    [SerializeField]
    private string displayName = "수상한 물건";

    [SerializeField]
    private bool isCrimeEvidence;

    [SerializeField, TextArea(2, 5)]
    private string resultDescription;

    [SerializeField]
    private Sprite resultImage;


    [Header("Events")]
    [SerializeField]
    private UnityEvent onClicked;


    public string DisplayName => displayName;

    public bool IsCrimeEvidence => isCrimeEvidence;

    public string ResultDescription => resultDescription;

    public Sprite ResultImage => resultImage;

    public bool IsInvestigated { get; private set; }


    /// <summary>
    /// 플레이어가 CCTV 화면에서 이 장소를 클릭했을 때
    /// 조사 UI를 연다.
    /// </summary>
    public void Interact()
    {
        if (!CCTVInspectionUI.TryOpen(this))
        {
            Debug.LogWarning(
                "CCTV Inspection UI가 씬에 없거나 설정되지 않았습니다.",
                this
            );
        }
    }


    /// <summary>
    /// 플레이어가 조사 확인 버튼을 눌러
    /// 실제 조사를 완료했을 때 호출된다.
    ///
    /// 이 시점에:
    /// 1. 조사 완료 상태 변경
    /// 2. 플레이어 행동 기록
    /// 3. 실제 단서라면 인벤토리에 이미지 추가
    /// 4. 기존 UnityEvent 실행
    /// 을 처리한다.
    /// </summary>
    public void CompleteInvestigation()
    {
        /*
         * 이미 조사한 장소라면
         * 같은 행동을 두 번 기록하지 않는다.
         */
        if (IsInvestigated)
        {
            return;
        }

        IsInvestigated = true;


        // =========================================================
        // 1. 이번 조사 결과가
        //    Clue / Decoy / Empty 중 무엇인지 판단
        // =========================================================

        InvestigationResultType resultType;

        /*
         * 이미지 자체가 없다면
         * AI가 아무 증거물도 배치하지 않은 빈 장소이다.
         */
        if (resultImage == null)
        {
            resultType =
                InvestigationResultType.Empty;
        }

        /*
         * 이미지가 있고,
         * 실제 범죄 단서로 지정된 경우
         */
        else if (isCrimeEvidence)
        {
            resultType =
                InvestigationResultType.Clue;
        }

        /*
         * 이미지가 있지만
         * 실제 범죄 단서가 아니라면 미끼이다.
         */
        else
        {
            resultType =
                InvestigationResultType.Decoy;
        }


        // =========================================================
        // 2. 이 조사 장소의 HideZone 찾기
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
            // =====================================================
            // 3. 씬의 PlayerActionRecorder를 찾아
            //    실제 조사 행동 저장
            // =====================================================

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
        // 4. 실제 범죄 단서(Clue)라면
        //    기존 인벤토리에 이미지 추가
        // =========================================================

        if (isCrimeEvidence)
        {
            EvidenceInventoryUI.TryCollect(
                resultImage
            );
        }


        // =========================================================
        // 5. 팀원이 기존에 연결해 둔 이벤트 실행
        // =========================================================

        onClicked?.Invoke();
    }


    /// <summary>
    /// AI가 HideZone에 EvidenceObject를 배치한 뒤
    /// 해당 증거물의 결과 정보를
    /// CCTVInteractable에 자동으로 연결한다.
    ///
    /// evidence가 null이면 빈 조사 장소로 설정한다.
    /// </summary>
    public void SetInvestigationResult(
        EvidenceObject evidence
    )
    {
        /*
         * 아무 증거물도 배치되지 않은 장소
         */
        if (evidence == null)
        {
            isCrimeEvidence = false;

            resultDescription =
                "특별한 흔적을 발견하지 못했습니다.";

            resultImage = null;

            return;
        }


        /*
         * 실제 범죄 단서인지 미끼인지 설정
         */
        isCrimeEvidence =
            evidence.EvidenceType ==
            EvidenceType.Clue;


        /*
         * 같은 EvidenceObject에 저장된
         * 설명과 이미지를 사용한다.
         */
        resultDescription =
            evidence.EvidenceDescription;

        resultImage =
            evidence.ResultImage;
    }
}