using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal static class BrickMaterialUrpFixer
{
    private const string AssetFolder =
        "Assets/MapTest/Assets/Brick Project Studio";
    private const string SessionKey = "SceneCleaner.BrickMaterialUrpFixer.V1";

    [InitializeOnLoadMethod]
    private static void ScheduleAutomaticFix()
    {
        if (SessionState.GetBool(SessionKey, false))
            return;

        SessionState.SetBool(SessionKey, true);
        EditorApplication.delayCall += FixMaterials;
    }

    [MenuItem("Tools/Scene Cleaner/Fix Brick Materials for URP")]
    private static void FixMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("URP Lit shader was not found.");
            return;
        }

        int changed = 0;
        string[] materialGuids = AssetDatabase.FindAssets(
            "t:Material", new[] { AssetFolder });

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string guid in materialGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || material.shader == urpLit)
                    continue;

                Dictionary<string, Texture> textures = ReadTextures(material);
                Dictionary<string, Color> colors = ReadColors(material);
                Dictionary<string, float> floats = ReadFloats(material);

                material.shader = urpLit;

                CopyTexture(textures, "_MainTex", material, "_BaseMap");
                CopyTexture(textures, "_BumpMap", material, "_BumpMap");
                CopyTexture(textures, "_MetallicGlossMap", material, "_MetallicGlossMap");
                CopyTexture(textures, "_EmissionMap", material, "_EmissionMap");
                CopyColor(colors, "_Color", material, "_BaseColor");
                CopyColor(colors, "_EmissionColor", material, "_EmissionColor");
                CopyFloat(floats, "_Metallic", material, "_Metallic");
                CopyFloat(floats, "_Glossiness", material, "_Smoothness");

                if (textures.TryGetValue("_BumpMap", out Texture normal) && normal != null)
                    material.EnableKeyword("_NORMALMAP");

                EditorUtility.SetDirty(material);
                changed++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Brick material URP fix complete: {changed} materials updated.");
    }

    private static Dictionary<string, Texture> ReadTextures(Material material)
    {
        var result = new Dictionary<string, Texture>();
        SerializedProperty entries = new SerializedObject(material)
            .FindProperty("m_SavedProperties.m_TexEnvs");

        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            string name = entry.FindPropertyRelative("first").stringValue;
            Texture value = entry.FindPropertyRelative("second.m_Texture")
                .objectReferenceValue as Texture;
            result[name] = value;
        }

        return result;
    }

    private static Dictionary<string, Color> ReadColors(Material material)
    {
        var result = new Dictionary<string, Color>();
        SerializedProperty entries = new SerializedObject(material)
            .FindProperty("m_SavedProperties.m_Colors");

        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            result[entry.FindPropertyRelative("first").stringValue] =
                entry.FindPropertyRelative("second").colorValue;
        }

        return result;
    }

    private static Dictionary<string, float> ReadFloats(Material material)
    {
        var result = new Dictionary<string, float>();
        SerializedProperty entries = new SerializedObject(material)
            .FindProperty("m_SavedProperties.m_Floats");

        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            result[entry.FindPropertyRelative("first").stringValue] =
                entry.FindPropertyRelative("second").floatValue;
        }

        return result;
    }

    private static void CopyTexture(
        Dictionary<string, Texture> values, string oldName,
        Material material, string newName)
    {
        if (values.TryGetValue(oldName, out Texture value) && value != null)
            material.SetTexture(newName, value);
    }

    private static void CopyColor(
        Dictionary<string, Color> values, string oldName,
        Material material, string newName)
    {
        if (values.TryGetValue(oldName, out Color value))
            material.SetColor(newName, value);
    }

    private static void CopyFloat(
        Dictionary<string, float> values, string oldName,
        Material material, string newName)
    {
        if (values.TryGetValue(oldName, out float value))
            material.SetFloat(newName, value);
    }
}
