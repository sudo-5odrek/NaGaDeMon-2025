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

    private GameObject previewObject;
    private BuildingData selectedBuilding;
    private bool isPlacing = false;
    private Camera cam;
    private InputSystem_Actions input;
    
    private float currentRotation = 0f;

    // --- Drag logic ---
    private bool isDragging = false;
    private Vector3 dragStartPos;
    private Vector3 dragCurrentPos;
    private IBuildPlacementLogic activePlacementLogic;

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
        if (!isPlacing)
            return;

        // --- Update preview position ---
        Vector3 mouseWorld = GetMouseWorldPosition();
        Vector3 snappedPos = SnapToGrid(mouseWorld);

        if (previewObject && selectedBuilding.placementMode == PlacementMode.Single)
            previewObject.transform.position = snappedPos;

        // --- Handle drag preview updates ---
        if (isDragging && selectedBuilding != null && selectedBuilding.placementMode == PlacementMode.Drag)
        {
            dragCurrentPos = snappedPos;
            activePlacementLogic?.OnDragging(dragCurrentPos);
        }

        // --- Range check coloring ---
        float dist = Vector2.Distance(snappedPos, player.position);
        SetPreviewColor(dist <= buildRange ? Color.green : Color.red);
    }

    // --------------------------------------------------
    // ROTATION
    // --------------------------------------------------

    private void OnRotatePerformed(InputAction.CallbackContext ctx)
    {
        if (!isPlacing || previewObject == null)
            return;

        float scrollValue = ctx.ReadValue<float>();
        if (Mathf.Abs(scrollValue) < 0.01f)
            return;

        // 🔹 Scroll up → rotate counter-clockwise
        // 🔹 Scroll down → rotate clockwise
        float direction = Mathf.Sign(scrollValue);
        currentRotation += direction * 90f;
        previewObject.transform.rotation = Quaternion.Euler(0, 0, currentRotation);
    }

    // --------------------------------------------------
    // PLACEMENT LOGIC
    // --------------------------------------------------

    public void StartPlacement(BuildingData building)
    {
        if (previewObject) Destroy(previewObject);

        selectedBuilding = building;
        isPlacing = true;
        currentRotation = 0f;

        if (selectedBuilding.placementMode == PlacementMode.Single)
        {
            previewObject = Instantiate(building.prefab);
            if (previewObject.TryGetComponent<Turret>(out var turret))
                turret.enabled = false;

            SetLayerRecursively(previewObject, LayerMask.NameToLayer("Ignore Raycast"));
            SetPreviewColor(Color.green);
        }
        else if (selectedBuilding.placementMode == PlacementMode.Drag)
        {
            activePlacementLogic = selectedBuilding.GetPlacementLogic();
            activePlacementLogic?.Setup(selectedBuilding.prefab, currentRotation);
        }

        InputContextManager.Instance.SetInputMode(InputContextManager.InputMode.Build);
    }

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

        // --- Single placement ---
        if (selectedBuilding.placementMode == PlacementMode.Single)
        {
            GameObject newBuild = Instantiate(selectedBuilding.prefab, snappedPos, Quaternion.Euler(0, 0, currentRotation));
            GridManager.Instance.BlockNodesUnderObject(newBuild);
            CancelPlacement();
            return;
        }

        // --- Drag placement ---
        if (selectedBuilding.placementMode == PlacementMode.Drag)
        {
            dragStartPos = snappedPos;
            isDragging = true;
            activePlacementLogic?.OnStartDrag(dragStartPos);
        }
    }

    private void OnPlaceCanceled(InputAction.CallbackContext ctx)
    {
        if (isDragging && selectedBuilding != null && selectedBuilding.placementMode == PlacementMode.Drag)
        {
            Vector3 dragEndPos = SnapToGrid(GetMouseWorldPosition());
            activePlacementLogic?.OnEndDrag(dragEndPos);
            isDragging = false;
            CancelPlacement();
        }
    }

    private void OnCancelPerformed(InputAction.CallbackContext ctx)
    {
        if (isPlacing)
            CancelPlacement();
    }

    private void CancelPlacement()
    {
        if (previewObject) Destroy(previewObject);
        selectedBuilding = null;
        isPlacing = false;
        isDragging = false;
        activePlacementLogic = null;

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

    private void SetPreviewColor(Color color)
    {
        if (!previewObject) return;

        foreach (var sr in previewObject.GetComponentsInChildren<SpriteRenderer>())
            sr.color = color;
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, newLayer);
    }
}
