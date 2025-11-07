using UnityEngine;
using UnityEngine.InputSystem;

public class BuildManager : MonoBehaviour
{
    public static BuildManager Instance;

    [Header("Settings")]
    public BuildingData[] availableBuildings;
    public float buildRange = 5f;

    [Header("References")]
    public Transform player;

    private BuildingData selectedBuilding;
    private bool isPlacing = false;
    private bool isDragging = false;
    private float currentRotation = 0f;

    private Camera cam;
    private InputSystem_Actions input;

    private IBuildPlacementLogic activePlacementLogic;

    private void Awake()
    {
        Instance = this;
        cam = Camera.main;
        input = InputContextManager.Instance.input;
    }

    private void OnEnable()
    {
        input.Player.Place.started += OnPlaceStarted;   // Mouse down
        input.Player.Place.canceled += OnPlaceCanceled; // Mouse up
        input.Player.Cancel.performed += OnCancelPerformed;
        input.Player.Rotate.performed += OnRotatePerformed;
    }

    private void OnDisable()
    {
        input.Player.Place.started -= OnPlaceStarted;
        input.Player.Place.canceled -= OnPlaceCanceled;
        input.Player.Cancel.performed -= OnCancelPerformed;
        input.Player.Rotate.performed -= OnRotatePerformed;
    }
    
    private void Update()
    {
        if (!isPlacing || activePlacementLogic == null)
            return;

        Vector3 snappedPos = SnapToGrid(GetMouseWorldPosition());
        float dist = Vector2.Distance(snappedPos, player.position);

        // --- Handle drag updates ---
        if (isDragging)
        {
            activePlacementLogic.OnDragging(snappedPos);
        }
        else
        {
            // --- Hover preview before click ---
            activePlacementLogic.UpdatePreview(snappedPos);
        }

        // Optional: Range check or coloring
        if (dist > buildRange)
        {
            // You could tell the logic to tint preview red, etc.
        }
    }


    // --------------------------------------------------
    // ROTATION
    // --------------------------------------------------

    private void OnRotatePerformed(InputAction.CallbackContext ctx)
    {
        if (!isPlacing)
            return;

        float scrollValue = ctx.ReadValue<float>();
        if (Mathf.Abs(scrollValue) < 0.01f)
            return;

        float direction = Mathf.Sign(scrollValue);
        currentRotation += direction * 90f;
    }

    // --------------------------------------------------
    // START PLACEMENT
    // --------------------------------------------------

    public void StartPlacement(BuildingData building)
    {
        selectedBuilding = building;
        isPlacing = true;
        currentRotation = 0f;

        // Load and initialize the correct placement logic
        activePlacementLogic = selectedBuilding.GetPlacementLogic();
        if (activePlacementLogic == null)
        {
            Debug.LogError($"No placement logic assigned for {building.name}");
            return;
        }

        activePlacementLogic.Setup(selectedBuilding.prefab, currentRotation);
        InputContextManager.Instance.SetInputMode(InputContextManager.InputMode.Build);
    }

    // --------------------------------------------------
    // INPUT HANDLING
    // --------------------------------------------------

    private void OnPlaceStarted(InputAction.CallbackContext ctx)
    {
        if (!isPlacing || selectedBuilding == null)
            return;

        Vector3 snappedPos = SnapToGrid(GetMouseWorldPosition());
        float dist = Vector2.Distance(snappedPos, player.position);
        if (dist > buildRange)
        {
            Debug.Log("❌ Too far from player to build here!");
            return;
        }

        isDragging = true;
        activePlacementLogic?.OnStartDrag(snappedPos);
    }

    private void OnPlaceCanceled(InputAction.CallbackContext ctx)
    {
        if (!isPlacing || selectedBuilding == null)
            return;

        Vector3 snappedPos = SnapToGrid(GetMouseWorldPosition());
        activePlacementLogic?.OnEndDrag(snappedPos);

        isDragging = false;
        CancelPlacement();
    }

    private void OnCancelPerformed(InputAction.CallbackContext ctx)
    {
        if (isPlacing)
            CancelPlacement();
    }

    // --------------------------------------------------
    // CLEANUP
    // --------------------------------------------------

    private void CancelPlacement()
    {
        activePlacementLogic?.ClearPreview();

        selectedBuilding = null;
        activePlacementLogic = null;
        isPlacing = false;
        isDragging = false;

        InputContextManager.Instance.SetInputMode(InputContextManager.InputMode.Normal);
    }

    // --------------------------------------------------
    // UTILITY
    // --------------------------------------------------

    private Vector3 GetMouseWorldPosition()
    {
        Vector2 mousePos = input.Player.Point.ReadValue<Vector2>();
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, -cam.transform.position.z));
        world.z = 0;
        return world;
    }

    private Vector3 SnapToGrid(Vector3 pos)
    {
        return GridManager.Instance.GetClosestNodeWorldPos(pos);
    }
}
