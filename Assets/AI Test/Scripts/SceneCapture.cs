using System;
using System.IO;
using UnityEngine;

public class SceneCapture : MonoBehaviour
{
    [SerializeField]
    private Camera captureCamera;

    [SerializeField]
    private int captureWidth = 960;

    [SerializeField]
    private int captureHeight = 540;

    private void Start()
    {
        Capture();
    }

    private void Capture()
    {
        if (captureCamera == null)
        {
            Debug.LogError("캡처할 카메라가 연결되지 않았습니다.");
            return;
        }

        RenderTexture renderTexture =
            new RenderTexture(captureWidth, captureHeight, 24);

        Texture2D capturedImage =
            new Texture2D(
                captureWidth,
                captureHeight,
                TextureFormat.RGB24,
                false
            );

        RenderTexture previousActiveTexture = RenderTexture.active;
        RenderTexture previousCameraTexture = captureCamera.targetTexture;

        captureCamera.targetTexture = renderTexture;
        captureCamera.Render();

        RenderTexture.active = renderTexture;

        capturedImage.ReadPixels(
            new Rect(0, 0, captureWidth, captureHeight),
            0,
            0
        );

        capturedImage.Apply();

        byte[] pngData = capturedImage.EncodeToPNG();

        string projectRoot =
            Directory.GetParent(Application.dataPath).FullName;

        string captureFolder =
            Path.Combine(projectRoot, "Captures");

        Directory.CreateDirectory(captureFolder);

        string fileName =
            $"capture_{DateTime.Now:yyyyMMdd_HHmmss}.png";

        string filePath =
            Path.Combine(captureFolder, fileName);

        File.WriteAllBytes(filePath, pngData);

        captureCamera.targetTexture = previousCameraTexture;
        RenderTexture.active = previousActiveTexture;

        renderTexture.Release();

        Destroy(renderTexture);
        Destroy(capturedImage);

        Debug.Log($"카메라 캡처 저장 완료: {filePath}");
    }
}