using UnityEngine;
using UnityEngine.InputSystem;

public class BuildManager : MonoBehaviour
{
    public static BuildManager Instance;

    [Header("Settings")]
    public BuildingData[] availableBuildings;
    public float buildRange = 5f;

    [Header("References")]
    public Transform player; // 🔹 Drag your player here in the Inspector

    private GameObject previewObject;
    private BuildingData selectedBuilding;
    private bool isPlacing = false;
    private Camera cam;
    private InputSystem_Actions input;

    private void Awake()
    {
        Instance = this;
        cam = Camera.main;
        input = InputContextManager.Instance.input;
    }

    private void OnEnable()
    {
        input.Player.Place.performed += OnPlacePerformed;
        input.Player.Cancel.performed += OnCancelPerformed;
    }

    private void OnDisable()
    {
        input.Player.Place.performed -= OnPlacePerformed;
        input.Player.Cancel.performed -= OnCancelPerformed;
    }

    private void Update()
    {
        if (!isPlacing) return;

        Vector3 mouseWorld = GetMouseWorldPosition();
        Vector3 snappedPos = SnapToGrid(mouseWorld);

        if (previewObject)
            previewObject.transform.position = snappedPos;

        float dist = Vector2.Distance(snappedPos, player.position);
        SetPreviewColor(dist <= buildRange ? Color.green : Color.red);
    }

    // --------------------------------------------------
    // BUILDING LOGIC
    // --------------------------------------------------

    public void StartPlacement(BuildingData building)
    {
        if (previewObject) Destroy(previewObject);

        selectedBuilding = building;
        previewObject = Instantiate(building.prefab);
        SetLayerRecursively(previewObject, LayerMask.NameToLayer("Ignore Raycast"));
        SetPreviewColor(Color.green);

        isPlacing = true;

        // Switch to build mode (disables attack input etc.)
        InputContextManager.Instance.SetInputMode(InputContextManager.InputMode.Build);
    }

    private void OnPlacePerformed(InputAction.CallbackContext ctx)
    {
        if (!isPlacing || selectedBuilding == null) return;

        Vector3 mouseWorld = GetMouseWorldPosition();
        Vector3 snappedPos = SnapToGrid(mouseWorld);
        float dist = Vector2.Distance(snappedPos, player.position);

        if (dist <= buildRange)
        {
            GameObject newBuild = Instantiate(selectedBuilding.prefab, snappedPos, Quaternion.identity);
            GridManager.Instance.BlockNodesUnderObject(newBuild);
            CancelPlacement();
        }
        else
        {
            Debug.Log("❌ Too far from player to build here!");
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

        // Return to normal input mode
        InputContextManager.Instance.SetInputMode(InputContextManager.InputMode.Normal);
    }

    // --------------------------------------------------
    // UTILITY
    // --------------------------------------------------

    private Vector3 GetMouseWorldPosition()
    {
        // Use your mouse position (Look or Point action)
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
