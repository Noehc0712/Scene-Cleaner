using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class CCTVCursorInteractor : MonoBehaviour
{
    [Header("Cursor")]
    [SerializeField] private Texture2D normalCursor;
    [SerializeField] private Vector2 normalCursorHotspot;
    [SerializeField] private Texture2D clickableCursor;
    [SerializeField] private Vector2 clickableCursorHotspot;

    [Header("Raycast")]
    [SerializeField] private LayerMask interactionLayers = ~0;
    [SerializeField, Min(0.1f)] private float maximumDistance = 100f;


    private Camera activeCamera;
    private CCTVInteractable hoveredInteractable;


    /// <summary>
    /// 현재 마우스가 올라가 있는 CCTVInteractable이
    /// 변경됐을 때 다른 스크립트에게 알려주는 이벤트.
    /// </summary>
    public static event Action<CCTVInteractable>
        HoveredInteractableChanged;


    /// <summary>
    /// 현재 마우스가 올라가 있는 조사 대상.
    /// 아무것도 없다면 null.
    /// </summary>
    public static CCTVInteractable CurrentHoveredInteractable
    {
        get;
        private set;
    }


    private void OnEnable()
    {
        Cursor.visible = true;

        ApplyCursor(
            normalCursor,
            normalCursorHotspot
        );
    }


    private void Update()
    {
        if (Mouse.current == null ||
            CCTVInspectionUI.IsModalOpen)
        {
            SetHovered(null);
            return;
        }


        RefreshActiveCamera();

        CCTVInteractable interactable =
            FindInteractableUnderCursor();

        SetHovered(interactable);


        if (interactable != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            GameAudioManager.PlayObjectClick();

            interactable.Interact();
        }
    }


    private void OnDisable()
    {
        SetHovered(null);
    }


    private void RefreshActiveCamera()
    {
        if (IsUsable(activeCamera))
        {
            return;
        }


        activeCamera = null;

        float highestDepth =
            float.NegativeInfinity;


        foreach (Camera camera in Camera.allCameras)
        {
            if (!IsUsable(camera) ||
                camera.depth < highestDepth)
            {
                continue;
            }

            activeCamera = camera;
            highestDepth = camera.depth;
        }
    }


    private CCTVInteractable FindInteractableUnderCursor()
    {
        if (!IsUsable(activeCamera))
        {
            return null;
        }


        Vector2 mousePosition =
            Mouse.current.position.ReadValue();

        Ray ray =
            activeCamera.ScreenPointToRay(
                mousePosition
            );


        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                maximumDistance,
                interactionLayers,
                QueryTriggerInteraction.Collide
            ))
        {
            return null;
        }


        CCTVInteractable interactable = hit.collider
            .GetComponentInParent<CCTVInteractable>();

        if (interactable == null ||
            !interactable.CanInteractFromCamera(
                CCTVSwitcher.CurrentCameraNumber
            ))
        {
            return null;
        }

        return interactable;
    }


    /// <summary>
    /// 현재 Hover 대상이 변경되었을 때
    /// 커서를 바꾸고 기록 시스템에 알려준다.
    /// </summary>
    private void SetHovered(
        CCTVInteractable interactable
    )
    {
        /*
         * 같은 물건 위에 계속 마우스가 있다면
         * 아무 처리도 하지 않는다.
         */
        if (hoveredInteractable == interactable)
        {
            return;
        }


        hoveredInteractable = interactable;

        CurrentHoveredInteractable =
            hoveredInteractable;


        ApplyCursor(
            hoveredInteractable != null
                ? clickableCursor
                : normalCursor,

            hoveredInteractable != null
                ? clickableCursorHotspot
                : normalCursorHotspot
        );


        /*
         * Hover 대상이 변경됐다는 사실을
         * PlayerHoverRecorder 등에 알린다.
         */
        HoveredInteractableChanged?.Invoke(
            CurrentHoveredInteractable
        );
    }


    private static void ApplyCursor(
        Texture2D texture,
        Vector2 hotspot
    )
    {
        Cursor.SetCursor(
            texture,
            hotspot,
            CursorMode.Auto
        );
    }


    private static bool IsUsable(Camera camera)
    {
        return camera != null &&
               camera.enabled &&
               camera.gameObject.activeInHierarchy;
    }
}
