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
    /// starting from a building, drag conveyor lines, accumulate tiles,
    /// finalize path when canceled or toggled off.
    /// </summary>
    public class ConnectionModeManager : MonoBehaviour
    {
        public static ConnectionModeManager Instance;

        [Header("Settings")]
        public GameObject connectionPrefab;           // single conveyor tile prefab
        public GameObject conveyorPathPrefab;         // root prefab (with ConveyorPathController)
        public ScriptableObject placementLogicAsset;  // must implement IBuildPlacementLogic
        public LayerMask buildingMask;
        public LayerMask connectionMask;

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
        // DESTRUCTION SYSTEM
        // --------------------------------------------------
        [Header("Destroy Settings")]
        public float destroyHoldTime = 1.0f;

        private float destroyTimer = 0f;
        private bool rightHeld = false;
        private ConveyorPathController hoveredController;


        // --------------------------------------------------
        // INIT
        // --------------------------------------------------
        private void Awake()
        {
            Instance = this;
            cam = Camera.main;
            input = InputContextManager.Instance.input;

            if (placementLogicAsset is IBuildPlacementLogic logic)
                placementLogic = Instantiate(placementLogicAsset) as IBuildPlacementLogic;
            else
                Debug.LogError($"[ConnectionModeManager] {placementLogicAsset.name} does NOT implement IBuildPlacementLogic!");
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

            Vector3 mouse = GetMouseWorldPosition();

            placementLogic.UpdatePreview(mouse);

            if (isBuildingChain && startPoint.HasValue)
                placementLogic.OnDragging(mouse);

            // --------------------------------------------------
            // HOLD-TO-DESTROY LOGIC
            // --------------------------------------------------
            HandleDestroyConveyor();
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

                placementLogic.Setup(connectionPrefab, 0f);

                if (placementLogic is ConveyorLinePlacementLogic lineLogic)
                    lineLogic.SetPlacementCallback(OnPlacementConfirmed);

                Debug.Log("🟢 Connection Mode ON");
            }
            else
            {
                FinalizeChain();      // finalize current chain
                ExitConnectionMode(); // and leave mode
            }
        }

        // --------------------------------------------------
        // DETECTING END OF EXISTING CONVEYOR
        // --------------------------------------------------
        private ConveyorPathController GetConveyorEndingAt(Vector3 mouseWorld)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(mouseWorld, 0.15f, connectionMask);

            foreach (var hit in hits)
            {
                ConveyorPathController controller = hit.GetComponentInParent<ConveyorPathController>();
                if (!controller) continue;

                if (controller.pathTiles == null || controller.pathTiles.Count == 0)
                    continue;

                GameObject lastTile = controller.pathTiles[^1];

                if (hit.gameObject == lastTile)
                    return controller;
            }

            return null;
        }
       
        // --------------------------------------------------
        // LEFT CLICK (start or extend)
        // --------------------------------------------------
        private void OnLeftClick(InputAction.CallbackContext ctx)
        {
            if (!isActive) return;

            Vector3 mouse = GetMouseWorldPosition();

            // STEP 1 — Start chain
            if (!isBuildingChain)
            {
                Vector3 snapped = SnapToGrid(mouse);

                // 1) Start from a building
                RaycastHit2D hit = Physics2D.Raycast(mouse, Vector2.zero, 0.1f, buildingMask);
                if (hit.collider && hit.collider.TryGetComponent(out BuildingInventory inventory))
                {
                    if (inventory.CanAcceptNewOutput)
                    {
                        GameObject root = Instantiate(conveyorPathPrefab);
                        currentController = root.GetComponent<ConveyorPathController>();

                        currentController.startInventory = null;
                        currentController.endInventory = null;

                        startBuilding = inventory;
                        startPoint = snapped;

                        placementLogic.OnStartDrag(startPoint.Value);
                        isBuildingChain = true;

                        Debug.Log($"🟩 Started new conveyor from building {startBuilding.name}");
                        return;
                    }
                }

                // 2) Extend existing conveyor
                ConveyorPathController target = GetConveyorEndingAt(mouse);

                if (target != null)
                {
                    currentController = target;

                    startBuilding = currentController.startInventory;

                    GameObject lastTile = currentController.pathTiles[^1];
                    startPoint = lastTile.transform.position;

                    placementLogic.OnStartDrag(startPoint.Value);
                    isBuildingChain = true;

                    Debug.Log($"🟦 Extending conveyor: {currentController.name}");
                    return;
                }

                return;
            }

            // STEP 2 — Extend chain
            if (isBuildingChain && startPoint.HasValue)
            {
                Vector3 endPoint = SnapToGrid(mouse);

                if (placementLogic is not ConveyorLinePlacementLogic lineLogic)
                    return;

                if (ValidateEndPoint(endPoint))
                {
                    lineLogic.GenerateTiles(startPoint.Value, endPoint, currentController.transform);

                    placementLogic.OnEndDrag(endPoint);

                    startPoint = endPoint;
                    placementLogic.OnStartDrag(startPoint.Value);

                    if (endBuilding)
                        FinalizeChain();
                }
            }
        }

        // --------------------------------------------------
        // RIGHT CLICK (finalize chain)
        // --------------------------------------------------
        private void OnRightClick(InputAction.CallbackContext ctx)
        {
            if (!isActive) return;

            Debug.Log("🛑 Conveyor chain finalized by player");
            FinalizeChain();
        }

        private bool ValidateEndPoint(Vector3 mouseWorld)
        {
            bool valid = true;

            Collider2D hit = Physics2D.OverlapCircle(mouseWorld, 0.1f, buildingMask);
            if (hit)
            {
                BuildingInventory inventory = hit.GetComponent<BuildingInventory>();

                if (inventory.CanAcceptNewInput)
                    endBuilding = inventory;
                else
                    valid = false;
            }

            return valid;
        }

        // --------------------------------------------------
        // CALLBACK FROM PLACEMENT LOGIC
        // --------------------------------------------------
        private void OnPlacementConfirmed(List<GameObject> tiles)
        {
            if (tiles == null || tiles.Count == 0) return;

            currentController.AddToPath(tiles);

            Vector3 a = tiles[0].transform.position;
            Vector3 b = tiles[^1].transform.position;

            Vector2 dir = (b - a).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            foreach (var t in tiles)
                t.transform.rotation = Quaternion.Euler(0, 0, angle);

            Debug.Log($"✅ Added {tiles.Count} conveyor tiles to {currentController.name}");
        }

        // --------------------------------------------------
        // FINALIZE
        // --------------------------------------------------
        private void FinalizeChain()
        {
            // finalize ports if applicable
            if (currentController != null && startBuilding != null)
            {
                if (currentController.pathTiles.Count > 0)
                {
                    var startPort = startBuilding?.GetOutput() as BuildingInventoryPort;
                    var endPort = endBuilding?.GetInput() as BuildingInventoryPort;

                    currentController.Initialize(startBuilding, startPort, endBuilding, endPort);
                }
            }

            // cleanup preview
            if (placementLogic is ConveyorLinePlacementLogic lineLogic)
                lineLogic.Cancel();
            else
                placementLogic.ClearPreview();

            // reset chain variables but DO NOT exit connection mode
            isBuildingChain = false;
            startPoint = null;
            startBuilding = null;
            endBuilding = null;
            currentController = null;

            Debug.Log("🔁 Chain finalized but staying in connection mode");
        }
        
        private void ExitConnectionMode()
        {
            InputContextManager.Instance.SetInputMode(InputContextManager.InputMode.Normal);

            isActive = false;
            isBuildingChain = false;
            startPoint = null;
            startBuilding = null;
            endBuilding = null;
            currentController = null;

            destroyTimer = 0f;
            hoveredController = null;

            Debug.Log("🔴 Conveyor Mode OFF");
        }


        // --------------------------------------------------
        // HOLD-TO-DESTROY CONVEYOR LINES
        // --------------------------------------------------
        private void HandleDestroyConveyor()
        {
            // never destroy while placing a chain
            if (isBuildingChain)
            {
                destroyTimer = 0f;
                hoveredController = null;
                return;
            }

            // detect right-click hold
            rightHeld = input.Player.Cancel.ReadValue<float>() > 0.5f;
            if (!rightHeld)
            {
                destroyTimer = 0f;
                hoveredController = null;
                return;
            }

            // detect conveyor under cursor
            ConveyorPathController controller = GetConveyorUnderCursor();
            if (controller == null)
            {
                destroyTimer = 0f;
                hoveredController = null;
                return;
            }

            hoveredController = controller;

            // count hold
            destroyTimer += Time.deltaTime;

            if (destroyTimer >= destroyHoldTime)
            {
                DestroyConveyorLine(hoveredController);
                destroyTimer = 0f;
                hoveredController = null;
            }
        }

        private ConveyorPathController GetConveyorUnderCursor()
        {
            Vector3 mouseWorld = GetMouseWorldPosition();

            Collider2D hit = Physics2D.OverlapCircle(mouseWorld, 0.15f, connectionMask);
            if (!hit)
                return null;

            return hit.GetComponentInParent<ConveyorPathController>();
        }

        private void DestroyConveyorLine(ConveyorPathController controller)
        {
            if (controller == null)
                return;

            Destroy(controller.gameObject);

            Debug.Log($"🔥 Destroyed conveyor line: {controller.name}");
        }


        // --------------------------------------------------
        // UTILS
        // --------------------------------------------------
        private Vector3 GetMouseWorldPosition()
        {
            Vector2 mouse = input.Player.Point.ReadValue<Vector2>();
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, -cam.transform.position.z));
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
