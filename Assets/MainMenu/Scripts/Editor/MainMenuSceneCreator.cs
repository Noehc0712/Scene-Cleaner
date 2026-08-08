using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal static class MainMenuSceneCreator
{
    private const string ScenePath = "Assets/Scenes/MainMenuScene.unity";
    private const string GameScenePath = "Assets/MapTest/MapTestScene.unity";

    [MenuItem("Tools/Scene Cleaner/Create Main Menu Scene")]
    private static void CreateMainMenuScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera();
        CreateEventSystem();

        Canvas canvas = CreateCanvas();
        MainMenuUI controller = canvas.gameObject.AddComponent<MainMenuUI>();

        Image background = CreateImage("Background", canvas.transform, new Color32(17, 21, 29, 255));
        Stretch(background.rectTransform);

        TMP_Text title = CreateText("Game Title", background.transform, "SCENE CLEANER", 54, FontStyles.Bold);
        SetRect(title.rectTransform, new Vector2(0.5f, 0.76f), new Vector2(700f, 90f));

        TMP_Text subtitle = CreateText("Subtitle", background.transform, "CCTV INVESTIGATION", 18, FontStyles.Normal);
        subtitle.color = new Color32(155, 180, 190, 255);
        SetRect(subtitle.rectTransform, new Vector2(0.5f, 0.68f), new Vector2(500f, 50f));

        VerticalLayoutGroup menu = CreateMenuLayout(background.transform);
        Button startButton = CreateButton("Start Game Button", menu.transform, "게임 시작");
        Button descriptionButton = CreateButton("Description Button", menu.transform, "게임 설명");
        Button settingsButton = CreateButton("Settings Button", menu.transform, "게임 설정");

        GameObject descriptionPanel = CreatePopupPanel(
            background.transform,
            "Game Description Panel",
            "게임 설명",
            "CCTV 화면을 전환하며 현장을 조사하고, 제한된 조사 기회 안에 범죄의 증거를 찾아내세요.");

        GameObject settingsPanel = CreatePopupPanel(
            background.transform,
            "Game Settings Panel",
            "게임 설정",
            "설정 항목은 추후 추가할 수 있습니다.");

        SetSerializedReference(controller, "descriptionPanel", descriptionPanel);
        SetSerializedReference(controller, "settingsPanel", settingsPanel);

        UnityEventTools.AddPersistentListener(startButton.onClick, controller.StartGame);
        UnityEventTools.AddPersistentListener(descriptionButton.onClick, controller.OpenDescription);
        UnityEventTools.AddPersistentListener(settingsButton.onClick, controller.OpenSettings);

        ConnectCloseButton(descriptionPanel, controller);
        ConnectCloseButton(settingsPanel, controller);

        descriptionPanel.SetActive(false);
        settingsPanel.SetActive(false);

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        UpdateBuildScenes();
        Selection.activeGameObject = canvas.gameObject;

        Debug.Log($"Main menu scene created: {ScenePath}");
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.cullingMask = 0;
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystem = new("EventSystem", typeof(EventSystem));
        System.Type inputModuleType = System.Type.GetType(
            "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");

        if (inputModuleType != null)
            eventSystem.AddComponent(inputModuleType);
        else
            eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new("Main Menu Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static VerticalLayoutGroup CreateMenuLayout(Transform parent)
    {
        GameObject menuObject = new("Menu Buttons", typeof(RectTransform), typeof(VerticalLayoutGroup));
        menuObject.transform.SetParent(parent, false);
        SetRect((RectTransform)menuObject.transform, new Vector2(0.5f, 0.38f), new Vector2(360f, 250f));

        VerticalLayoutGroup layout = menuObject.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        return layout;
    }

    private static Button CreateButton(string name, Transform parent, string label)
    {
        GameObject buttonObject = new(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color32(42, 54, 66, 245);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color32(70, 92, 105, 255);
        colors.pressedColor = new Color32(26, 34, 42, 255);
        button.colors = colors;

        LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
        layout.preferredHeight = 70f;

        TMP_Text text = CreateText("Label", buttonObject.transform, label, 26, FontStyles.Bold);
        Stretch(text.rectTransform, 12f);
        return button;
    }

    private static GameObject CreatePopupPanel(
        Transform parent, string name, string titleText, string bodyText)
    {
        Image dimmer = CreateImage(name, parent, new Color(0f, 0f, 0f, 0.72f));
        Stretch(dimmer.rectTransform);

        Image window = CreateImage("Window", dimmer.transform, new Color32(35, 43, 52, 255));
        SetRect(window.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(760f, 430f));

        TMP_Text title = CreateText("Title", window.transform, titleText, 34, FontStyles.Bold);
        SetRect(title.rectTransform, new Vector2(0.5f, 0.78f), new Vector2(650f, 70f));

        TMP_Text body = CreateText("Body", window.transform, bodyText, 23, FontStyles.Normal);
        body.alignment = TextAlignmentOptions.Center;
        SetRect(body.rectTransform, new Vector2(0.5f, 0.52f), new Vector2(620f, 150f));

        Button close = CreateButton("Close Button", window.transform, "닫기");
        RectTransform closeRect = close.GetComponent<RectTransform>();
        SetRect(closeRect, new Vector2(0.5f, 0.2f), new Vector2(220f, 64f));
        Object.DestroyImmediate(close.GetComponent<LayoutElement>());
        return dimmer.gameObject;
    }

    private static void ConnectCloseButton(GameObject panel, MainMenuUI controller)
    {
        Button closeButton = panel.GetComponentInChildren<Button>(true);
        UnityEventTools.AddPersistentListener(closeButton.onClick, controller.ClosePanels);
    }

    private static TMP_Text CreateText(
        string name, Transform parent, string content, float fontSize, FontStyles style)
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        return text;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = new(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static void SetSerializedReference(Object target, string propertyName, Object value)
    {
        SerializedObject serializedObject = new(target);
        serializedObject.FindProperty(propertyName).objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect, float margin = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(margin, margin);
        rect.offsetMax = new Vector2(-margin, -margin);
    }

    private static void UpdateBuildScenes()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true)
        };
    }
}
