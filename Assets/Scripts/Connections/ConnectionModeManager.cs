using System.Collections.Generic;
using Building;
using Building.Conveyer;
using Grid;
using Inventory;
using Placement_Logics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Connections
{
    /// <summary>
    /// Handles connection (conveyor) placement mode:
    /// start from a building, draw conveyor lines, accumulate all tiles,
    /// and finalize the path when canceled or toggled off.
    /// </summary>
    public class ConnectionModeManager : MonoBehaviour
    {
        public static ConnectionModeManager Instance;

        [Header("Settings")]
        public GameObject connectionPrefab;   // single conveyor tile prefab
        public GameObject conveyorPathPrefab; // root prefab with ConveyorPathController
        public ScriptableObject placementLogicAsset;
        public LayerMask buildingMask;

        [Header("References")]
        public Transform player;

        private IBuildPlacementLogic placementLogic;
        private Camera cam;
        private InputSystem_Actions input;

        private bool isActive;
        private bool isBuildingChain;

        private Vector3? startPoint;
        private BuildingInventory startBuilding;
        private BuildingInventory endBuilding;
        private ConveyorPathController currentController;

        // --------------------------------------------------
        // INIT
        // --------------------------------------------------
        private void Awake()
        {
            Instance = this;
            cam = Camera.main;
            input = InputContextManager.Instance.input;

            if (placementLogicAsset is IBuildPlacementLogic)
                placementLogic = Instantiate(placementLogicAsset) as IBuildPlacementLogic;
            else
                Debug.LogError($"[ConnectionModeManager] {placementLogicAsset.name} does not implement IBuildPlacementLogic!");
        }

        private void OnEnable()
        {
            input.Player.ConnectMode.performed += OnToggleMode;
            input.Player.Place.started += OnLeftClick;
            input.Player.Cancel.performed += OnRightClick;
        }

        private void OnDisable()
        {
            input.Player.ConnectMode.performed -= OnToggleMode;
            input.Player.Place.started -= OnLeftClick;
            input.Player.Cancel.performed -= OnRightClick;
        }

        private void Update()
        {
            if (!isActive) return;

            Vector3 mouseWorld = GetMouseWorldPosition();

            // Always update hover ghost
            placementLogic.UpdatePreview(mouseWorld);

            // Draw line preview if currently chaining
            if (isBuildingChain && startPoint.HasValue)
                placementLogic.OnDragging(mouseWorld);
        }

        // --------------------------------------------------
        // MODE TOGGLE
        // --------------------------------------------------
        private void OnToggleMode(InputAction.CallbackContext ctx)
        {
            isActive = !isActive;

            if (isActive)
            {
                InputContextManager.Instance.SetInputMode(InputContextManager.InputMode.Connect);
                placementLogic?.Setup(connectionPrefab, 0f, createPreview: true);

                if (placementLogic is ConveyorLinePlacementLogic lineLogic)
                    lineLogic.SetPlacementCallback(OnPlacementConfirmed);

                Debug.Log("🟢 Connection Mode ON");
            }
            else
            {
                FinalizeAndExit();
            }
        }

        // --------------------------------------------------
        // LEFT CLICK (start or extend)
        // --------------------------------------------------
        private void OnLeftClick(InputAction.CallbackContext ctx)
        {
            if (!isActive) return;
            Vector3 mouseWorld = GetMouseWorldPosition();

            // STEP 1: start chain from a building
            if (!isBuildingChain)
            {
                RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero, 0.1f, buildingMask);
                if (hit.collider && hit.collider.TryGetComponent(out BuildingInventory inventory))
                {
                    startBuilding = inventory;
                    startPoint = SnapToGrid(mouseWorld);

                    // create conveyor root + controller
                    GameObject pathRoot = Instantiate(conveyorPathPrefab);
                    pathRoot.name = $"ConveyorPath_{inventory.name}";
                    currentController = pathRoot.GetComponent<ConveyorPathController>();

                    // ✅ Register start based on building reference
                    var startPort = startBuilding.GetOutput();

                    placementLogic.OnStartDrag(startPoint.Value);
                    isBuildingChain = true;

                    Debug.Log($"🟩 Started conveyor chain from {startBuilding.name}");
                }
                return;
            }

            // STEP 2: extend current chain
            if (isBuildingChain && startPoint.HasValue)
            {
                Vector3 endPoint = SnapToGrid(mouseWorld);

                if (placementLogic is not ConveyorLinePlacementLogic lineLogic)
                    return;

                List<GameObject> newTiles = lineLogic.GenerateTiles(startPoint.Value, endPoint, currentController.transform);

                placementLogic.OnEndDrag(endPoint);
                startPoint = endPoint;
                placementLogic.OnStartDrag(startPoint.Value);
            }
        }

        // --------------------------------------------------
        // RIGHT CLICK (finalize)
        // --------------------------------------------------
        private void OnRightClick(InputAction.CallbackContext ctx)
        {
            if (!isActive) return;
            Debug.Log("🛑 Conveyor chain finalized by player.");
            FinalizeAndExit();
        }

        // --------------------------------------------------
        // CALLBACK FROM PLACEMENT LOGIC
        // --------------------------------------------------
        private void OnPlacementConfirmed(List<GameObject> placedObjects)
        {
            if (placedObjects == null || placedObjects.Count == 0) return;

            if (currentController)
                currentController.AddToPath(placedObjects);

            // ✅ Determine overall segment direction
            Vector3 start = placedObjects[0].transform.position;
            Vector3 end = placedObjects[^1].transform.position;
            Vector2 dir = (end - start).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // ✅ Apply same rotation to all tiles in the segment
            foreach (var tile in placedObjects)
                tile.transform.rotation = Quaternion.Euler(0, 0, angle);

            Debug.Log($"✅ Segment confirmed: {placedObjects.Count} tiles added to {currentController.name}");
        }


        // --------------------------------------------------
        // FINALIZATION (driven by start building)
        // --------------------------------------------------
        private void FinalizeAndExit()
        {
            if (currentController != null && startBuilding != null)
            {
                Debug.Log("<UNK> Conveyor chain finalized by player.");
                // find end building at last tile
                if (currentController.pathTiles.Count > 0)
                {
                    GameObject lastTile = currentController.pathTiles[^1];
                    Collider2D hit = Physics2D.OverlapCircle(lastTile.transform.position, 0.1f, buildingMask);
                    
                    if (hit)
                    {
                        endBuilding = hit.GetComponent<BuildingInventory>();
                    }
                    
                    var startPort = startBuilding?.GetOutput() as BuildingInventoryPort;
                    var endPort = endBuilding?.GetInput() as BuildingInventoryPort;
                    currentController.Initialize(startBuilding, startPort, endBuilding, endPort);
                }
            }

            // cleanup visuals
            if (placementLogic is ConveyorLinePlacementLogic lineLogic)
                lineLogic.FinalizePath();
            else
                placementLogic?.ClearPreview();

            InputContextManager.Instance.SetInputMode(InputContextManager.InputMode.Normal);

            isActive = false;
            isBuildingChain = false;
            startPoint = null;
            startBuilding = null;
            currentController = null;

            Debug.Log("🔴 Connection Mode OFF");
        }

        // --------------------------------------------------
        // UTILITIES
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
            (int gx, int gy) = GridManager.Instance.GridFromWorld(pos);
            return GridManager.Instance.WorldFromGrid(gx, gy);
        }
    }
}
