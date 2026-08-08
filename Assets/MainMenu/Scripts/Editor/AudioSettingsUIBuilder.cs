using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal static class AudioSettingsUIBuilder
{
    private const string SettingsPanelName = "Game Settings Panel";
    private const string GeneratedRootName = "Audio Settings Controls";

    [MenuItem("Tools/Scene Cleaner/Build Audio Settings UI")]
    private static void BuildAudioSettingsUI()
    {
        GameObject settingsPanel = FindSettingsPanel();
        if (settingsPanel == null)
        {
            EditorUtility.DisplayDialog(
                "Audio Settings UI",
                $"현재 씬에서 '{SettingsPanelName}'을 찾지 못했습니다.\nMainMenuScene을 열고 다시 실행해 주세요.",
                "확인");
            return;
        }

        Transform parent = settingsPanel.transform.Find("Window") ?? settingsPanel.transform;
        Transform existingRoot = parent.Find(GeneratedRootName);
        if (existingRoot != null)
        {
            Selection.activeGameObject = existingRoot.gameObject;
            EditorUtility.DisplayDialog(
                "Audio Settings UI",
                "이미 생성된 Audio Settings Controls가 있습니다.\n다시 만들려면 해당 오브젝트만 삭제한 후 실행해 주세요.",
                "확인");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Audio Settings UI");

        GameObject root = CreateUIObject(GeneratedRootName, parent, typeof(AudioSettingsUI));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        SetRect(rootRect, new Vector2(0.5f, 0.5f), new Vector2(660f, 220f), new Vector2(0f, -5f));

        Transform placeholderBody = parent.Find("Body");
        if (placeholderBody != null)
        {
            Undo.RecordObject(placeholderBody.gameObject, "Hide Settings Placeholder");
            placeholderBody.gameObject.SetActive(false);
        }

        Slider musicSlider = CreateAudioRow(
            root.transform, "Background Music", "배경음악", new Vector2(0f, 45f), out TMP_Text musicValue);
        Slider effectSlider = CreateAudioRow(
            root.transform, "Sound Effects", "효과음", new Vector2(0f, -45f), out TMP_Text effectValue);

        AudioSettingsUI settingsUI = root.GetComponent<AudioSettingsUI>();
        SerializedObject serializedUI = new(settingsUI);
        serializedUI.FindProperty("backgroundMusicSlider").objectReferenceValue = musicSlider;
        serializedUI.FindProperty("soundEffectSlider").objectReferenceValue = effectSlider;
        serializedUI.FindProperty("backgroundMusicValueText").objectReferenceValue = musicValue;
        serializedUI.FindProperty("soundEffectValueText").objectReferenceValue = effectValue;
        serializedUI.ApplyModifiedPropertiesWithoutUndo();

        Undo.RegisterCreatedObjectUndo(root, "Build Audio Settings UI");
        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeGameObject = root;
        EditorUtility.SetDirty(settingsPanel);

        Debug.Log("Audio Settings UI 생성 및 연결 완료.", root);
    }

    private static GameObject FindSettingsPanel()
    {
        if (Selection.activeGameObject != null &&
            Selection.activeGameObject.scene == SceneManager.GetActiveScene())
        {
            Transform current = Selection.activeGameObject.transform;
            while (current != null)
            {
                if (current.name == SettingsPanelName)
                    return current.gameObject;

                current = current.parent;
            }
        }

        Scene activeScene = SceneManager.GetActiveScene();
        foreach (Transform candidate in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (candidate.gameObject.scene == activeScene && candidate.name == SettingsPanelName)
                return candidate.gameObject;
        }

        return null;
    }

    private static Slider CreateAudioRow(
        Transform parent,
        string name,
        string labelText,
        Vector2 position,
        out TMP_Text valueText)
    {
        GameObject row = CreateUIObject(name + " Row", parent);
        SetRect(row.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(610f, 75f), position);

        TMP_Text label = CreateText("Label", row.transform, labelText, 23f, FontStyles.Normal);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        SetRect(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(155f, 50f), new Vector2(-220f, 0f));

        Slider slider = CreateSlider("Slider", row.transform);
        SetRect(slider.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(330f, 34f), new Vector2(35f, 0f));

        valueText = CreateText("Value", row.transform, "100%", 20f, FontStyles.Normal);
        valueText.color = new Color32(155, 205, 205, 255);
        SetRect(valueText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(80f, 45f), new Vector2(250f, 0f));
        return slider;
    }

    private static Slider CreateSlider(string name, Transform parent)
    {
        GameObject sliderObject = CreateUIObject(name, parent, typeof(Slider));
        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.wholeNumbers = false;

        Image background = CreateImage("Background", sliderObject.transform, new Color32(18, 25, 29, 255));
        Stretch(background.rectTransform, 0f, 8f);

        GameObject fillArea = CreateUIObject("Fill Area", sliderObject.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        Stretch(fillAreaRect, 10f, 10f);

        Image fill = CreateImage("Fill", fillArea.transform, new Color32(80, 170, 170, 255));
        Stretch(fill.rectTransform);

        GameObject handleArea = CreateUIObject("Handle Slide Area", sliderObject.transform);
        Stretch(handleArea.GetComponent<RectTransform>(), 12f, 0f);

        Image handle = CreateImage("Handle", handleArea.transform, new Color32(210, 230, 225, 255));
        SetRect(handle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(22f, 34f), Vector2.zero);

        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private static TMP_Text CreateText(
        string name, Transform parent, string content, float fontSize, FontStyles style)
    {
        GameObject textObject = CreateUIObject(name, parent, typeof(TextMeshProUGUI));
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        return text;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = CreateUIObject(name, parent, typeof(Image));
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static GameObject CreateUIObject(string name, Transform parent, params System.Type[] components)
    {
        System.Type[] types = new System.Type[components.Length + 1];
        types[0] = typeof(RectTransform);
        components.CopyTo(types, 1);

        GameObject gameObject = new(name, types);
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void SetRect(
        RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void Stretch(
        RectTransform rect, float horizontalMargin = 0f, float verticalMargin = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(horizontalMargin, verticalMargin);
        rect.offsetMax = new Vector2(-horizontalMargin, -verticalMargin);
    }
}
