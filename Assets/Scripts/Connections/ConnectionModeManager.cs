using System.Collections.Generic;
using Building;
using Building.Conveyer;
using Grid;
using Interface;
using Inventory;
using Placement_Logics;
using UnityEngine;
using UnityEngine.InputSystem;
using Player;

namespace Connections
{
    /// <summary>
    /// Handles conveyor placement mode:
    /// start from a building, drag conveyor lines, confirm segments on click,
    /// cost checking included.
    /// </summary>
    public class ConnectionModeManager : MonoBehaviour
    {
        public static ConnectionModeManager Instance;

        [Header("Settings")]
        public GameObject connectionPrefab;           // conveyor tile prefab
        public GameObject conveyorPathPrefab;         // path root prefab
        public ScriptableObject placementLogicAsset;  // must implement IBuildPlacementLogic
        public LayerMask buildingMask;
        public LayerMask connectionMask;

        [Header("Cost Settings")]
        public BuildingData conveyorBuildingData;     // assign conveyor building cost here

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
        // DESTRUCTION
        // --------------------------------------------------
        [Header("Destroy Settings")]
        public float destroyHoldTime = 1.0f;

        private float destroyTimer = 0f;
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
            {
                placementLogic = Instantiate(placementLogicAsset) as IBuildPlacementLogic;
            }
            else
            {
                Debug.LogError($"[ConnectionModeManager] {placementLogicAsset.name} does NOT implement IBuildPlacementLogic!");
            }
        }

        private void OnEnable()
        {
            input.Player.ConnectMode.performed += OnToggleMode;
            input.Player.Place.started += OnLeftClick;
            input.Player.Cancel.performed += OnRightClick;
            
            PlayerInventory.Instance.OnInventoryChanged += OnInventoryChanged;
        }

        private void OnDisable()
        {
            input.Player.ConnectMode.performed -= OnToggleMode;
            input.Player.Place.started -= OnLeftClick;
            input.Player.Cancel.performed -= OnRightClick;
            
            PlayerInventory.Instance.OnInventoryChanged -= OnInventoryChanged;
        }
        
        private void OnInventoryChanged()
        {
            if (!isActive || placementLogic == null) return;

            UpdateAffordabilityVisuals();
        }
        
        private void UpdateAffordabilityVisuals()
        {
            if (conveyorBuildingData == null) return;

            int count = placementLogic.GetPreviewCount();
            bool canAfford = CanAfford(conveyorBuildingData, count);

            Color tint = canAfford ? Color.green : Color.red;

            placementLogic.SetGhostColor(tint);
            UIPlacementCostIndicator.Instance.SetColor(tint);

            placementLogic.UpdateCostPreview(conveyorBuildingData);
        }

        private void Update()
        {
            if (!isActive) return;

            Vector3 mouse = GetMouseWorldPosition();

            placementLogic.UpdatePreview(mouse);

            if (isBuildingChain && startPoint.HasValue)
                placementLogic.OnDragging(mouse);

            // Update cost preview every frame
            if (conveyorBuildingData != null)
                placementLogic.UpdateCostPreview(conveyorBuildingData);
            
            UpdateAffordabilityVisuals();

            HandleDestroyConveyor();
        }

        // --------------------------------------------------
        // COST SYSTEM
        // --------------------------------------------------
        private bool CanAfford(BuildingData building, int count = 1)
        {
            if (!building) return false;

            var inv = PlayerInventory.Instance;

            foreach (var c in building.cost)
            {
                int required = c.amount * count;
                if (inv.GetAmount(c.item) < required)
                    return false;
            }

            return true;
        }

        private void SpendCost(BuildingData building, int count = 1)
        {
            var inv = PlayerInventory.Instance;

            foreach (var c in building.cost)
            {
                int total = c.amount * count;
                inv.RemoveItem(c.item, total);
            }
        }

        // Public helper used by placement logic if needed
        public bool CanAffordConveyor(int count)
        {
            return CanAfford(conveyorBuildingData, count);
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
                {
                    lineLogic.SetPlacementCallback(OnPlacementConfirmed);
                }

                Debug.Log("🟢 Conveyor Mode ON");
            }
            else
            {
                FinalizeChain();
                ExitConnectionMode();
            }
        }

        // --------------------------------------------------
        // LEFT CLICK — PLACE SEGMENT
        // --------------------------------------------------
        private void OnLeftClick(InputAction.CallbackContext ctx)
        {
            if (!isActive) return;

            Vector3 mouse = GetMouseWorldPosition();

            // STEP 1 — begin chain
            if (!isBuildingChain)
            {
                Vector3 snapped = SnapToGrid(mouse);

                // Start from building output
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

                        Debug.Log($"🟩 Started conveyor from {startBuilding.name}");
                        return;
                    }
                }

                // Extend existing conveyor
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

            // STEP 2 — place second or subsequent segment
            if (isBuildingChain && startPoint.HasValue)
            {
                Vector3 endPoint = SnapToGrid(mouse);

                if (placementLogic is not ConveyorLinePlacementLogic lineLogic)
                    return;

                // Determine building count BEFORE placing
                int tileCount = lineLogic.GetPreviewCount();

                // ❗ HARD COST CHECK HERE ❗
                if (!CanAfford(conveyorBuildingData, tileCount))
                {
                    Debug.Log("❌ Not enough resources to place this conveyor segment!");
                    placementLogic.AbortDrag();
                    return; // DO NOT PLACE ANYTHING
                }

                // Generate tiles
                lineLogic.GenerateTiles(startPoint.Value, endPoint, currentController.transform);

                // Spend cost
                SpendCost(conveyorBuildingData, tileCount);

                placementLogic.OnEndDrag(endPoint);

                // Continue chaining
                startPoint = endPoint;
                placementLogic.OnStartDrag(startPoint.Value);

                if (endBuilding)
                    FinalizeChain();
            }
        }

        // --------------------------------------------------
        // EXISTING CONVEYOR DETECTION
        // --------------------------------------------------
        private ConveyorPathController GetConveyorEndingAt(Vector3 mouseWorld)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(mouseWorld, 0.15f, connectionMask);

            foreach (var hit in hits)
            {
                ConveyorPathController controller = hit.GetComponentInParent<ConveyorPathController>();
                if (!controller) continue;

                if (controller.pathTiles.Count == 0)
                    continue;

                GameObject lastTile = controller.pathTiles[^1];

                if (hit.gameObject == lastTile)
                    return controller;
            }

            return null;
        }

        // --------------------------------------------------
        // RIGHT CLICK — finalize conveyor
        // --------------------------------------------------
        private void OnRightClick(InputAction.CallbackContext ctx)
        {
            if (!isActive) return;

            Debug.Log("🛑 Player finalized conveyor chain");
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

            Debug.Log($"📦 Added {tiles.Count} conveyor tiles");
        }

        // --------------------------------------------------
        // FINALIZE CHAIN
        // --------------------------------------------------
        private void FinalizeChain()
        {
            if (currentController != null && startBuilding != null)
            {
                if (currentController.pathTiles.Count > 0)
                {
                    var startPort = startBuilding?.GetOutput() as BuildingInventoryPort;
                    currentController.Initialize(startBuilding, startPort, endBuilding);
                }
            }

            placementLogic.OnEndDrag(Vector3.zero);

            // reset
            isBuildingChain = false;
            startPoint = null;
            startBuilding = null;
            endBuilding = null;
            currentController = null;

            Debug.Log("🔁 Conveyor chain ended (but staying in connection mode)");
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
        // DESTRUCTION
        // --------------------------------------------------
        private void HandleDestroyConveyor()
        {
            if (isBuildingChain)
            {
                destroyTimer = 0f;
                hoveredController = null;
                return;
            }

            bool rightHeld = input.Player.Cancel.ReadValue<float>() > 0.5f;

            if (!rightHeld)
            {
                destroyTimer = 0f;
                hoveredController = null;
                return;
            }

            ConveyorPathController controller = GetConveyorUnderCursor();
            if (controller == null)
            {
                destroyTimer = 0f;
                hoveredController = null;
                return;
            }

            hoveredController = controller;
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
            return hit ? hit.GetComponentInParent<ConveyorPathController>() : null;
        }

        private void DestroyConveyorLine(ConveyorPathController controller)
        {
            if (controller == null) return;

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
            var (gx, gy) = GridManager.Instance.GridFromWorld(pos);
            return GridManager.Instance.WorldFromGrid(gx, gy);
        }
    }
}
