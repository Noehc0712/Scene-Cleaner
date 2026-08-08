using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CCTVStaticTransition : MonoBehaviour
{
    [SerializeField] private RawImage noiseOverlay;
    [SerializeField, Range(0.05f, 0.8f)] private float duration = 0.2f;
    [SerializeField, Range(0.05f, 1f)] private float maximumOpacity = 0.55f;
    [SerializeField, Range(32, 320)] private int textureWidth = 160;
    [SerializeField, Range(18, 180)] private int textureHeight = 90;

    private Texture2D noiseTexture;
    private Color32[] pixels;
    private Coroutine transitionRoutine;

    public bool IsPlaying => transitionRoutine != null;

    private void Awake()
    {
        if (noiseOverlay == null)
        {
            Debug.LogError("CCTV Static Transition의 Noise Overlay를 연결해 주세요.", this);
            enabled = false;
            return;
        }

        noiseTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
        {
            name = "Runtime CCTV Noise",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat
        };
        pixels = new Color32[textureWidth * textureHeight];
        noiseOverlay.texture = noiseTexture;
        noiseOverlay.raycastTarget = false;
        noiseOverlay.gameObject.SetActive(false);
    }

    public bool Play(Action switchCamera)
    {
        if (!enabled || IsPlaying)
            return false;

        GameAudioManager.PlayCCTVStatic(duration);
        transitionRoutine = StartCoroutine(PlayRoutine(switchCamera));
        return true;
    }

    private IEnumerator PlayRoutine(Action switchCamera)
    {
        noiseOverlay.gameObject.SetActive(true);
        float elapsed = 0f;
        bool cameraSwitched = false;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            if (!cameraSwitched && progress >= 0.5f)
            {
                cameraSwitched = true;
                switchCamera?.Invoke();
            }

            UpdateNoise();
            float pulse = 1f - Mathf.Abs(progress * 2f - 1f);
            Color color = noiseOverlay.color;
            color.a = maximumOpacity * Mathf.Lerp(0.35f, 1f, pulse);
            noiseOverlay.color = color;
            yield return null;
        }

        if (!cameraSwitched)
            switchCamera?.Invoke();

        noiseOverlay.gameObject.SetActive(false);
        transitionRoutine = null;
    }

    private void UpdateNoise()
    {
        for (int y = 0; y < textureHeight; y++)
        {
            bool brightScanline = UnityEngine.Random.value < 0.035f;
            for (int x = 0; x < textureWidth; x++)
            {
                byte value = (byte)UnityEngine.Random.Range(25, 235);
                if (brightScanline)
                    value = (byte)Mathf.Min(255, value + 55);

                pixels[y * textureWidth + x] = new Color32(value, value, value, 255);
            }
        }

        noiseTexture.SetPixels32(pixels);
        noiseTexture.Apply(false);
    }

    private void OnDestroy()
    {
        if (noiseTexture != null)
            Destroy(noiseTexture);
    }
}
