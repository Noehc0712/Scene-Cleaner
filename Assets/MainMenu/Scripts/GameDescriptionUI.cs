using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class GameDescriptionUI : MonoBehaviour
{
    [Header("Page 1 - World")]
    [SerializeField] private string documentTitle = "SCENE CLEANER / 사건 브리핑";
    [SerializeField, TextArea(10, 18)] private string worldDescription =
        "AI가 인간의 일상과 판단을 대신하기 시작한 가까운 미래. " +
        "기술은 도시를 더 안전하고 편리하게 만들었지만, 동시에 범죄의 방식도 바꾸어 놓았습니다.\n\n" +
        "범죄 조직은 불법 현장 은폐 AI 'SCENE CLEANER'를 이용합니다. " +
        "이 AI는 CCTV 기록을 분석해 범죄와 관련된 물건을 평범한 생활용품처럼 위장하고, " +
        "자동 수사 시스템이 의심할 만한 흔적을 노이즈와 일상적인 장면 속에 감춥니다.\n\n" +
        "그러나 AI는 물건이 놓인 이유와 사람의 행동, 사건 전후의 맥락까지 완벽하게 이해하지 못합니다. " +
        "기계가 정상이라고 판정한 화면을 다시 의심하고 최종 판단을 내리는 일은 인간 조사관에게 남아 있습니다.\n\n" +
        "당신은 제한된 CCTV 영상만을 전달받은 특별 조사관입니다. " +
        "SCENE CLEANER가 감춘 단서를 직접 찾아내고, 조작된 기록 너머의 진실을 밝혀내십시오.";

    [Header("Page 2 - Control Images (Optional)")]
    [SerializeField] private Sprite cameraSwitchImage;
    [SerializeField] private Sprite cursorMoveImage;
    [SerializeField] private Sprite investigateImage;
    [SerializeField, TextArea(2, 4)] private string controlsDescription =
        "조사 기회는 총 3번입니다. 평범해 보이는 물건도 자세히 관찰하고, 확실한 단서를 선택하세요.";

    [Header("Layout")]
    [SerializeField] private Texture paperTexture;
    [SerializeField] private Vector2 paperSize = new(980f, 820f);
    [SerializeField] private Color inkColor = new(0.15f, 0.12f, 0.09f, 1f);

    private Action closeAction;
    [SerializeField, HideInInspector] private GameObject worldPage;
    [SerializeField, HideInInspector] private GameObject controlsPage;
    [SerializeField, HideInInspector] private Button nextButton;
    [SerializeField, HideInInspector] private Button previousButton;
    [SerializeField, HideInInspector] private Button closeButton;

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && !ResolveBakedReferences())
            UnityEditor.EditorApplication.delayCall += BakeLayoutInEditor;
#endif
    }

    private void Awake()
    {
        if (!Application.isPlaying)
            return;

        ResolveBakedReferences();
        nextButton?.onClick.AddListener(ShowControlsPage);
        previousButton?.onClick.AddListener(ShowPreviousPage);
        closeButton?.onClick.AddListener(Close);
    }

    public void Configure(Texture texture, Action onClose)
    {
        if (texture != null)
            paperTexture = texture;
        closeAction = onClose;

    }

    public void ShowFirstPage()
    {
        if (worldPage != null && controlsPage != null)
            ShowPage(worldPage);
    }

#if UNITY_EDITOR
    [ContextMenu("Bake Editable Layout Into Scene")]
    private void BakeLayoutInEditor()
    {
        if (this == null || Application.isPlaying)
            return;

        RectTransform window = transform.Find("Window") as RectTransform;
        if (window == null)
        {
            Debug.LogError("게임 설명 패널에서 Window 오브젝트를 찾을 수 없습니다.", this);
            return;
        }

        TMP_FontAsset koreanFont = FindExistingFont(window);
        for (int i = window.childCount - 1; i >= 0; i--)
        {
            GameObject child = window.GetChild(i).gameObject;
            DestroyImmediate(child);
        }

        window.sizeDelta = paperSize;
        if (window.TryGetComponent(out Image oldBackground))
            oldBackground.enabled = false;

        RawImage paper = CreatePaper(window);
        worldPage = CreatePage("World Page", paper.transform);
        controlsPage = CreatePage("Controls Page", paper.transform);
        BuildWorldPage(worldPage.transform, koreanFont);
        BuildControlsPage(controlsPage.transform, koreanFont);

        ShowPage(worldPage);
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif

    private void BuildWorldPage(Transform page, TMP_FontAsset font)
    {
        CreateText("Title", page, documentTitle, font, 39f, FontStyles.Bold,
            new Vector2(0.08f, 0.85f), new Vector2(0.92f, 0.95f), TextAlignmentOptions.Center);
        CreateText("World Description", page, worldDescription, font, 23f, FontStyles.Normal,
            new Vector2(0.10f, 0.18f), new Vector2(0.90f, 0.83f), TextAlignmentOptions.TopLeft);

        nextButton = CreateButton(page, "다음 페이지  〉",
            new Vector2(0.64f, 0.055f), new Vector2(0.90f, 0.13f), font);
    }

    private void BuildControlsPage(Transform page, TMP_FontAsset font)
    {
        CreateText("Title", page, "조사관 조작 안내", font, 39f, FontStyles.Bold,
            new Vector2(0.08f, 0.85f), new Vector2(0.92f, 0.95f), TextAlignmentOptions.Center);

        CreateControlRow(page, cameraSwitchImage, "1 · 2 · 3 · 4", "CCTV 카메라 전환",
            0.63f, 0.82f, 1f, font);
        CreateControlRow(page, cursorMoveImage, "MOUSE MOVE", "화면 속 조사 대상 확인",
            0.43f, 0.62f, 0.65f, font);
        CreateControlRow(page, investigateImage, "LEFT CLICK", "수상한 물건 조사",
            0.23f, 0.42f, 0.65f, font);

        CreateText("Attempt Notice", page, controlsDescription, font, 21f, FontStyles.Bold,
            new Vector2(0.12f, 0.14f), new Vector2(0.88f, 0.22f), TextAlignmentOptions.Center);

        previousButton = CreateButton(page, "〈  이전 페이지",
            new Vector2(0.10f, 0.055f), new Vector2(0.36f, 0.13f), font);

        closeButton = CreateButton(page, "설명 닫기",
            new Vector2(0.64f, 0.055f), new Vector2(0.90f, 0.13f), font);
    }

    private void CreateControlRow(
        Transform parent, Sprite sprite, string placeholder, string description,
        float anchorMinY, float anchorMaxY, float imageScale, TMP_FontAsset font)
    {
        GameObject slotObject = new("Control Image Slot", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        slotObject.transform.SetParent(parent, false);
        slotObject.transform.localScale = Vector3.one * imageScale;
        RectTransform slotRect = slotObject.GetComponent<RectTransform>();
        SetAnchors(slotRect, new Vector2(0.12f, anchorMinY), new Vector2(0.40f, anchorMaxY));

        Image image = slotObject.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = sprite != null ? Color.white : new Color(0.22f, 0.18f, 0.13f, 0.18f);
        image.raycastTarget = false;

        if (sprite == null)
            CreateText("Placeholder", slotObject.transform, placeholder, font, 20f, FontStyles.Bold,
                Vector2.zero, Vector2.one, TextAlignmentOptions.Center);

        CreateText("Description", parent, description, font, 28f, FontStyles.Bold,
            new Vector2(0.45f, anchorMinY), new Vector2(0.88f, anchorMaxY),
            TextAlignmentOptions.MidlineLeft);
    }

    private RawImage CreatePaper(Transform parent)
    {
        GameObject paperObject = new("Paper Document", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(RawImage));
        paperObject.transform.SetParent(parent, false);
        Stretch(paperObject.GetComponent<RectTransform>());

        RawImage image = paperObject.GetComponent<RawImage>();
        image.texture = paperTexture;
        image.color = paperTexture != null ? Color.white : new Color(0.88f, 0.82f, 0.68f, 1f);
        return image;
    }

    private static GameObject CreatePage(string name, Transform parent)
    {
        GameObject page = new(name, typeof(RectTransform));
        page.transform.SetParent(parent, false);
        Stretch(page.GetComponent<RectTransform>());
        return page;
    }

    private Button CreateButton(
        Transform parent, string labelText, Vector2 anchorMin, Vector2 anchorMax, TMP_FontAsset font)
    {
        GameObject buttonObject = new(labelText, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        SetAnchors(buttonObject.GetComponent<RectTransform>(), anchorMin, anchorMax);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.20f, 0.16f, 0.11f, 0.92f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.36f, 0.28f, 0.18f, 1f);
        colors.pressedColor = new Color(0.12f, 0.09f, 0.06f, 1f);
        button.colors = colors;

        TMP_Text label = CreateText("Label", buttonObject.transform, labelText, font,
            21f, FontStyles.Bold, Vector2.zero, Vector2.one, TextAlignmentOptions.Center);
        label.color = new Color(0.92f, 0.86f, 0.72f, 1f);
        return button;
    }

    private TMP_Text CreateText(
        string name, Transform parent, string content, TMP_FontAsset font,
        float fontSize, FontStyles style, Vector2 anchorMin, Vector2 anchorMax,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new(name, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        SetAnchors(textObject.GetComponent<RectTransform>(), anchorMin, anchorMax);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = content;
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = inkColor;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.lineSpacing = 7f;
        text.raycastTarget = false;
        return text;
    }

    private void ShowPage(GameObject page)
    {
        worldPage.SetActive(page == worldPage);
        controlsPage.SetActive(page == controlsPage);
    }

    private void ShowControlsPage()
    {
        GameAudioManager.PlayButtonClick();

        if (worldPage != null && controlsPage != null)
            ShowPage(controlsPage);
    }

    private void ShowPreviousPage()
    {
        GameAudioManager.PlayButtonClick();
        ShowFirstPage();
    }

    private bool ResolveBakedReferences()
    {
        Transform window = transform.Find("Window");
        Transform paper = window != null ? window.Find("Paper Document") : null;

        if (worldPage == null && paper != null)
        {
            Transform page = paper.Find("World Page");
            if (page != null)
                worldPage = page.gameObject;
        }

        if (controlsPage == null && paper != null)
        {
            Transform page = paper.Find("Controls Page");
            if (page != null)
                controlsPage = page.gameObject;
        }

        if (worldPage != null && nextButton == null)
            nextButton = worldPage.GetComponentInChildren<Button>(true);

        if (controlsPage != null && (previousButton == null || closeButton == null))
        {
            Button[] buttons = controlsPage.GetComponentsInChildren<Button>(true);
            if (buttons.Length >= 2)
            {
                previousButton = buttons[0];
                closeButton = buttons[1];
            }
        }

        return worldPage != null && controlsPage != null &&
               nextButton != null && previousButton != null && closeButton != null;
    }

    private void Close() => closeAction?.Invoke();

    private static TMP_FontAsset FindExistingFont(Transform root)
    {
        TMP_Text existingText = root.GetComponentInChildren<TMP_Text>(true);
        return existingText != null ? existingText.font : null;
    }

    private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Stretch(RectTransform rect) => SetAnchors(rect, Vector2.zero, Vector2.one);
}
