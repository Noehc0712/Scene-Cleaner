using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

internal static class GameTimerHUDBuilder
{
    private const string GameSceneName = "MapTestScene";

    [MenuItem("Tools/Scene Cleaner/Create Editable Game Timer HUD")]
    private static void CreateEditableTimer()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != GameSceneName)
        {
            EditorUtility.DisplayDialog(
                "Game Timer HUD",
                "MapTestScene을 연 상태에서 실행해 주세요.",
                "확인");
            return;
        }

        GameTimerHUD timer = Object.FindAnyObjectByType<GameTimerHUD>(
            FindObjectsInactive.Include);
        if (timer == null)
        {
            GameObject timerObject = new(nameof(GameTimerHUD));
            Undo.RegisterCreatedObjectUndo(timerObject, "Create Editable Game Timer HUD");
            timer = Undo.AddComponent<GameTimerHUD>(timerObject);
        }

        Transform existingCanvas = timer.transform.Find("Game Timer Canvas");
        if (existingCanvas != null)
        {
            Selection.activeGameObject = existingCanvas.gameObject;
            EditorGUIUtility.PingObject(existingCanvas.gameObject);
            return;
        }

        Canvas canvas = CreateCanvas(timer.transform);
        Image panel = CreatePanel(canvas.transform);
        TMP_Text timerText = CreateTimerText(panel.transform);

        SerializedObject serializedTimer = new(timer);
        serializedTimer.FindProperty("timerText").objectReferenceValue = timerText;
        serializedTimer.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = panel.gameObject;
        EditorGUIUtility.PingObject(panel.gameObject);
        EditorSceneManager.MarkSceneDirty(activeScene);

        Debug.Log("씬에서 직접 편집 가능한 Game Timer HUD UI가 생성되었습니다.", timer);
    }

    private static Canvas CreateCanvas(Transform parent)
    {
        GameObject canvasObject = new(
            "Game Timer Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create Timer Canvas");
        canvasObject.transform.SetParent(parent, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static Image CreatePanel(Transform parent)
    {
        GameObject panelObject = new("Timer Panel", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(panelObject, "Create Timer Panel");
        panelObject.transform.SetParent(parent, false);

        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-38f, -32f);
        rect.sizeDelta = new Vector2(230f, 82f);

        Image image = panelObject.GetComponent<Image>();
        image.color = new Color(0.015f, 0.025f, 0.03f, 0.82f);
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateTimerText(Transform parent)
    {
        GameObject textObject = new("Timer Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(textObject, "Create Timer Text");
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(12f, 6f);
        rect.offsetMax = new Vector2(-12f, -6f);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = "00:30";
        text.fontSize = 42f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color32(205, 232, 229, 255);
        text.raycastTarget = false;
        return text;
    }
}
