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

    private void OnEnable()
    {
        Cursor.visible = true;
        ApplyCursor(normalCursor, normalCursorHotspot);
    }

    private void Update()
    {
        if (Mouse.current == null || CCTVInspectionUI.IsModalOpen)
        {
            SetHovered(null);
            return;
        }

        RefreshActiveCamera();
        CCTVInteractable interactable = FindInteractableUnderCursor();
        SetHovered(interactable);

        if (interactable != null && Mouse.current.leftButton.wasPressedThisFrame)
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
            return;

        activeCamera = null;
        float highestDepth = float.NegativeInfinity;

        foreach (Camera camera in Camera.allCameras)
        {
            if (!IsUsable(camera) || camera.depth < highestDepth)
                continue;

            activeCamera = camera;
            highestDepth = camera.depth;
        }
    }

    private CCTVInteractable FindInteractableUnderCursor()
    {
        if (!IsUsable(activeCamera))
            return null;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = activeCamera.ScreenPointToRay(mousePosition);

        if (!Physics.Raycast(
                ray, out RaycastHit hit, maximumDistance,
                interactionLayers, QueryTriggerInteraction.Collide))
        {
            return null;
        }

        CCTVInteractable interactable =
            hit.collider.GetComponentInParent<CCTVInteractable>();

        if (interactable == null ||
            !interactable.CanInteractFromCamera(CCTVSwitcher.CurrentCameraNumber))
        {
            return null;
        }

        return interactable;
    }

    private void SetHovered(CCTVInteractable interactable)
    {
        if (hoveredInteractable == interactable)
            return;

        hoveredInteractable = interactable;
        ApplyCursor(
            hoveredInteractable != null ? clickableCursor : normalCursor,
            hoveredInteractable != null ? clickableCursorHotspot : normalCursorHotspot);
    }

    private static void ApplyCursor(Texture2D texture, Vector2 hotspot)
    {
        Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
    }

    private static bool IsUsable(Camera camera)
    {
        return camera != null &&
               camera.enabled &&
               camera.gameObject.activeInHierarchy;
    }
}
