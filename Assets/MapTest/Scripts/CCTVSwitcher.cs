using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class CCTVSwitcher : MonoBehaviour
{
    private const string FirstCameraName = "CCTV_01";
    private const string SecondCameraName = "CCTV_02";
    private const string ThirdCameraName = "CCTV_03";
    private const string FourthCameraName = "CCTV_04";

    private Camera firstCamera;
    private Camera secondCamera;
    private Camera thirdCamera;
    private Camera fourthCamera;
    private CCTVStaticTransition staticTransition;

    public static event Action<int> ActiveCameraChanged;
    public static int CurrentCameraNumber { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadedCallback()
    {
        // Remove first so entering Play Mode with domain reload disabled
        // cannot register the same callback more than once.
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForInitialScene()
    {
        TryCreateForLoadedScene();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreateForLoadedScene();
    }

    private static void TryCreateForLoadedScene()
    {
        if (FindAnyObjectByType<CCTVSwitcher>() != null ||
            FindSceneCamera(FirstCameraName) == null ||
            FindSceneCamera(SecondCameraName) == null)
            return;

        var controller = new GameObject(nameof(CCTVSwitcher));
        controller.AddComponent<CCTVSwitcher>();
    }

    private void Start()
    {
        staticTransition = FindAnyObjectByType<CCTVStaticTransition>();
        firstCamera = FindSceneCamera(FirstCameraName);
        secondCamera = FindSceneCamera(SecondCameraName);
        thirdCamera = FindSceneCamera(ThirdCameraName);
        fourthCamera = FindSceneCamera(FourthCameraName);

        if (firstCamera == null || secondCamera == null)
        {
            Debug.LogError(
                $"CCTV 전환 실패: 씬에 Camera 컴포넌트가 있는 " +
                $"'{FirstCameraName}'과 '{SecondCameraName}' 오브젝트가 모두 필요합니다.");
            enabled = false;
            return;
        }

        DisableOtherSceneCameras();
        SwitchTo(firstCamera);
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame ||
            Keyboard.current.numpad1Key.wasPressedThisFrame)
        {
            RequestSwitch(firstCamera);
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame ||
                 Keyboard.current.numpad2Key.wasPressedThisFrame)
        {
            RequestSwitch(secondCamera);
        }
        else if (Keyboard.current.digit3Key.wasPressedThisFrame ||
                 Keyboard.current.numpad3Key.wasPressedThisFrame)
        {
            RequestSwitch(thirdCamera);
        }
        else if (Keyboard.current.digit4Key.wasPressedThisFrame ||
                 Keyboard.current.numpad4Key.wasPressedThisFrame)
        {
            RequestSwitch(fourthCamera);
        }
    }

    private void RequestSwitch(Camera target)
    {
        if (target == null || target.gameObject.activeInHierarchy)
            return;

        if (staticTransition == null || !staticTransition.Play(() => SwitchTo(target)))
            SwitchTo(target);
    }

    private void SwitchTo(Camera target)
    {
        if (target == null)
            return;

        SetCameraActive(firstCamera, target == firstCamera);
        SetCameraActive(secondCamera, target == secondCamera);
        SetCameraActive(thirdCamera, target == thirdCamera);
        SetCameraActive(fourthCamera, target == fourthCamera);

        int cameraNumber = GetCameraNumber(target);
        if (cameraNumber != CurrentCameraNumber)
        {
            CurrentCameraNumber = cameraNumber;
            ActiveCameraChanged?.Invoke(CurrentCameraNumber);
        }
    }

    private int GetCameraNumber(Camera camera)
    {
        if (camera == firstCamera)
            return 1;
        if (camera == secondCamera)
            return 2;
        if (camera == thirdCamera)
            return 3;
        if (camera == fourthCamera)
            return 4;

        return 0;
    }

    private static void SetCameraActive(Camera camera, bool active)
    {
        if (camera == null)
            return;

        camera.gameObject.SetActive(active);
        camera.enabled = active;

        if (!camera.TryGetComponent<AudioListener>(out var listener) && active)
            listener = camera.gameObject.AddComponent<AudioListener>();

        if (listener != null)
            listener.enabled = active;
    }

    private void DisableOtherSceneCameras()
    {
        foreach (Camera camera in Resources.FindObjectsOfTypeAll<Camera>())
        {
            if (!IsLoadedSceneObject(camera.gameObject) ||
                camera == firstCamera || camera == secondCamera ||
                camera == thirdCamera || camera == fourthCamera)
            {
                continue;
            }

            camera.enabled = false;

            if (camera.TryGetComponent<AudioListener>(out var listener))
                listener.enabled = false;
        }
    }

    private static Camera FindSceneCamera(string objectName)
    {
        foreach (Camera camera in Resources.FindObjectsOfTypeAll<Camera>())
        {
            if (IsLoadedSceneObject(camera.gameObject) && camera.name == objectName)
                return camera;
        }

        return null;
    }

    private static bool IsLoadedSceneObject(GameObject gameObject)
    {
        Scene scene = gameObject.scene;
        return scene.IsValid() && scene.isLoaded;
    }

    private void OnDestroy()
    {
        CurrentCameraNumber = 0;
    }
}
