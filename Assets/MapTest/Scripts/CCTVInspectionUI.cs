using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CCTVInspectionUI : MonoBehaviour
{
    [Header("Attempts")]
    [SerializeField, Min(1)] private int maximumAttempts = 3;

    [Header("Confirmation Panel")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private TMP_Text confirmationMessage;
    [SerializeField] private TMP_Text attemptsText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitle;
    [SerializeField] private TMP_Text resultDescription;
    [SerializeField] private Image resultImage;
    [SerializeField] private Button closeButton;

    private static CCTVInspectionUI instance;
    private CCTVInteractable selectedObject;
    private int remainingAttempts;

    public static bool IsModalOpen => instance != null &&
                                      (instance.confirmationPanel.activeSelf ||
                                       instance.resultPanel.activeSelf);

    private void Awake()
    {
        if (!HasRequiredReferences())
        {
            Debug.LogError(
                "CCTVInspectionUI의 패널, 텍스트, 버튼, 이미지 참조를 모두 연결해 주세요.",
                this);
            enabled = false;
            return;
        }

        if (instance != null && instance != this)
        {
            Debug.LogError("CCTVInspectionUI는 씬에 하나만 있어야 합니다.", this);
            enabled = false;
            return;
        }

        instance = this;
        remainingAttempts = maximumAttempts;

        yesButton.onClick.AddListener(ConfirmInvestigation);
        noButton.onClick.AddListener(CancelInvestigation);
        closeButton.onClick.AddListener(CloseResult);

        CloseAll();
    }

    private bool HasRequiredReferences()
    {
        return confirmationPanel != null &&
               confirmationMessage != null &&
               attemptsText != null &&
               yesButton != null &&
               noButton != null &&
               resultPanel != null &&
               resultTitle != null &&
               resultDescription != null &&
               resultImage != null &&
               closeButton != null;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public static bool TryOpen(CCTVInteractable interactable)
    {
        if (instance == null || !instance.enabled || interactable == null)
            return false;

        instance.Open(interactable);
        return true;
    }

    private void Open(CCTVInteractable interactable)
    {
        selectedObject = interactable;

        if (selectedObject.IsInvestigated)
        {
            ShowResult(selectedObject);
            return;
        }

        resultPanel.SetActive(false);
        confirmationPanel.SetActive(true);
        confirmationMessage.text =
            $"{selectedObject.DisplayName}{GetObjectParticle(selectedObject.DisplayName)} 정말로 확인하시겠습니까?";
        attemptsText.text = $"탐사 기회 {remainingAttempts}/{maximumAttempts}";
        yesButton.interactable = remainingAttempts > 0;
    }

    private static string GetObjectParticle(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return "을";

        char lastCharacter = word.TrimEnd()[^1];

        // 한글 완성형 음절은 종성 인덱스가 0이면 받침이 없다.
        if (lastCharacter is >= '\uAC00' and <= '\uD7A3')
            return (lastCharacter - '\uAC00') % 28 == 0 ? "를" : "을";

        return "을";
    }

    private void ConfirmInvestigation()
    {
        if (selectedObject == null || selectedObject.IsInvestigated || remainingAttempts <= 0)
            return;

        GameAudioManager.PlayButtonClick();
        remainingAttempts--;
        selectedObject.CompleteInvestigation();
        ShowResult(selectedObject);
    }

    private void CancelInvestigation()
    {
        GameAudioManager.PlayButtonClick();
        CloseAll();
    }

    private void CloseResult()
    {
        GameAudioManager.PlayButtonClick();
        CloseAll();
    }

    private void ShowResult(CCTVInteractable interactable)
    {
        confirmationPanel.SetActive(false);
        resultPanel.SetActive(true);

        resultTitle.text = interactable.IsCrimeEvidence
            ? "범죄 흔적을 찾았다"
            : "조사 결과";
        resultDescription.text = interactable.ResultDescription;

        bool hasImage = interactable.ResultImage != null;
        resultImage.gameObject.SetActive(hasImage);
        resultImage.sprite = interactable.ResultImage;
        resultImage.preserveAspect = true;
    }

    private void CloseAll()
    {
        selectedObject = null;
        confirmationPanel.SetActive(false);
        resultPanel.SetActive(false);
    }
}
