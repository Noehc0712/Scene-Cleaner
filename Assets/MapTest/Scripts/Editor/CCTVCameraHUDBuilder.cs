using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal static class CCTVCameraHUDBuilder
{
    private const string GameSceneName = "MapTestScene";

    [MenuItem("Tools/Scene Cleaner/Create Editable CCTV Indicator HUD")]
    private static void CreateEditableHUD()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != GameSceneName)
        {
            EditorUtility.DisplayDialog(
                "CCTV Indicator HUD",
                "MapTestScene을 연 상태에서 실행해 주세요.",
                "확인");
            return;
        }

        CCTVCameraHUD hud = Object.FindAnyObjectByType<CCTVCameraHUD>(FindObjectsInactive.Include);
        if (hud == null)
        {
            GameObject hudObject = new(nameof(CCTVCameraHUD));
            Undo.RegisterCreatedObjectUndo(hudObject, "Create CCTV Indicator HUD");
            hud = Undo.AddComponent<CCTVCameraHUD>(hudObject);
        }

        Transform existingCanvas = hud.transform.Find("CCTV Indicator Canvas");
        if (existingCanvas != null)
        {
            Selection.activeGameObject = existingCanvas.gameObject;
            EditorGUIUtility.PingObject(existingCanvas.gameObject);
            return;
        }

        Canvas canvas = CreateCanvas(hud.transform);
        TMP_Text text = CreateText(canvas.transform);

        SerializedObject serializedHUD = new(hud);
        serializedHUD.FindProperty("cameraText").objectReferenceValue = text;
        serializedHUD.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = text.gameObject;
        EditorGUIUtility.PingObject(text.gameObject);
        EditorSceneManager.MarkSceneDirty(activeScene);
        Debug.Log("편집 가능한 CCTV Indicator HUD가 생성되었습니다.", hud);
    }

    private static Canvas CreateCanvas(Transform parent)
    {
        GameObject canvasObject = new(
            "CCTV Indicator Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create CCTV Indicator Canvas");
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

    private static TMP_Text CreateText(Transform parent)
    {
        GameObject textObject = new("CCTV Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(textObject, "Create CCTV Text");
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(40f, 34f);
        rect.sizeDelta = new Vector2(260f, 60f);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = "CCTV 01";
        text.fontSize = 30f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = new Color32(190, 220, 216, 255);
        text.raycastTarget = false;
        return text;
    }
}
