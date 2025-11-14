using UnityEngine;
using UnityEngine.InputSystem;
using Grid;
using Building;

public class BuildManager : MonoBehaviour
{
    public static BuildManager Instance;

    [Header("Settings")]
    public BuildingData[] availableBuildings;
    public float buildRange = 5f;

    [Header("References")]
    public Transform player;
    public BuildMenuUI buildMenu;

    private BuildingData selectedBuilding;
    private bool isPlacing = false;
    private bool isDragging = false;
    private bool validDragStarted = false;

    private float currentRotation = 0f;

    private Camera cam;
    private InputSystem_Actions input;
    private IBuildPlacementLogic activePlacementLogic;

    // --------------------------------------------------
    // INITIALIZATION
    // --------------------------------------------------

    private void Awake()
    {
        Instance = this;
        cam = Camera.main;
        input = InputContextManager.Instance.input;
    }

    private void OnEnable()
    {
        input.Player.Place.started += OnPlaceStarted;
        input.Player.Place.canceled += OnPlaceCanceled;
        input.Player.Cancel.performed += OnCancelPerformed;   // Right-click
        input.Player.Rotate.performed += OnRotatePerformed;
        input.Player.BuildMenu.performed += OnMenuPerformed;    // E key
    }

    private void OnDisable()
    {
        input.Player.Place.started -= OnPlaceStarted;
        input.Player.Place.canceled -= OnPlaceCanceled;
        input.Player.Cancel.performed -= OnCancelPerformed;
        input.Player.Rotate.performed -= OnRotatePerformed;
        input.Player.BuildMenu.performed -= OnMenuPerformed;
    }

    // --------------------------------------------------
    // UPDATE LOOP
    // --------------------------------------------------

    private void Update()
    {
        if (!isPlacing || activePlacementLogic == null)
            return;

        Vector3 snappedPos = SnapToGrid(GetMouseWorldPosition());
        float dist = Vector2.Distance(snappedPos, player.position);

        if (isDragging)
        {
            activePlacementLogic.OnDragging(snappedPos);
        }
        else
        {
            activePlacementLogic.UpdatePreview(snappedPos);

            // 🔴 Optional visual feedback (if implemented in placement logic)
            // Example: activePlacementLogic.SetPreviewTint(dist > buildRange ? Color.red : Color.white);
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
        isDragging = false;
        validDragStarted = false;
        currentRotation = 0f;

        activePlacementLogic = selectedBuilding.GetPlacementLogic();
        if (activePlacementLogic == null)
        {
            Debug.LogError($"No placement logic assigned for {building.name}");
            return;
        }

        activePlacementLogic.Setup(selectedBuilding.prefab, currentRotation, true);
        InputContextManager.Instance.SetInputMode(InputContextManager.InputMode.Build);

        buildMenu?.Hide(); // hide menu while placing
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
        Node startNode = GridManager.Instance.GetClosestNode(snappedPos);

        // 🚫 Invalid start: too far or unwalkable
        if (dist > buildRange || startNode == null || !startNode.walkable)
        {
            validDragStarted = false;
            Debug.Log("❌ Invalid drag start (out of range or unwalkable).");
            return;
        }

        // ✅ Begin valid drag
        isDragging = true;
        validDragStarted = true;
        activePlacementLogic?.OnStartDrag(snappedPos);
    }

    private void OnPlaceCanceled(InputAction.CallbackContext ctx)
    {
        if (!isPlacing || selectedBuilding == null)
            return;

        // 🚫 Skip invalid or aborted drags
        if (!validDragStarted)
        {
            isDragging = false;
            validDragStarted = false;
            return;
        }

        Vector3 snappedPos = SnapToGrid(GetMouseWorldPosition());
        float dist = Vector2.Distance(snappedPos, player.position);
        Node endNode = GridManager.Instance.GetClosestNode(snappedPos);

        // 🚫 Ignore invalid end positions
        if (dist > buildRange || endNode == null || !endNode.walkable)
        {
            Debug.Log("❌ Invalid drag end (out of range or unwalkable).");
            isDragging = false;
            validDragStarted = false;
            return;
        }

        // ✅ Valid placement
        activePlacementLogic?.OnEndDrag(snappedPos);

        isDragging = false;
        validDragStarted = false;

        // ✅ Recreate hover preview for next placement
        StartCoroutine(RecreatePreviewNextFrame());
    }
    
    private System.Collections.IEnumerator RecreatePreviewNextFrame()
    {
        yield return null; // wait one frame
        if (isPlacing && selectedBuilding != null)
            activePlacementLogic?.Setup(selectedBuilding.prefab, currentRotation, true);
    }

    private void OnCancelPerformed(InputAction.CallbackContext ctx)
    {
        if (InputContextManager.Instance.CurrentMode != InputContextManager.InputMode.Build)
            return;
        
        // 🖱️ Right-click behavior depends on current state
        if (isPlacing)
        {
            // Clear preview and FULLY EXIT build mode
            activePlacementLogic?.ClearPreview();
            ExitBuildMode();
            buildMenu?.Show();
        }
        else
        {
            // ✅ Close menu if open
            buildMenu.Hide();
            InputContextManager.Instance.SetInputMode(InputContextManager.InputMode.Normal);
        }
    }

    private void OnMenuPerformed(InputAction.CallbackContext ctx)
    {
        // ⌨️ E pressed
        if (InputContextManager.Instance.CurrentMode == InputContextManager.InputMode.Build)
        {
            // ✅ Cancel placement and close everything
            activePlacementLogic?.ClearPreview();
            ExitBuildMode();
            buildMenu?.Hide();
            InputContextManager.Instance.SetInputMode(InputContextManager.InputMode.Normal);
            return;
        }
        
        InputContextManager.Instance.SetInputMode(InputContextManager.InputMode.Build);
        buildMenu.Show();
    }

    // --------------------------------------------------
    // STATE MANAGEMENT
    // --------------------------------------------------

    private void ExitBuildMode()
    {
        activePlacementLogic?.ClearPreview();
        selectedBuilding = null;
        activePlacementLogic = null;
        isPlacing = false;
        isDragging = false;
        validDragStarted = false;
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
