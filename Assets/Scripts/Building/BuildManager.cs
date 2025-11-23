using Grid;
using Interface;
using Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Building
{
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

        private float currentRotation = 0f;

        private Camera cam;
        private InputSystem_Actions input;
        private IBuildPlacementLogic activePlacementLogic;

        // ------------------------------------------------------------
        // DESTRUCTION SYSTEM
        // ------------------------------------------------------------
        [Header("Destroy Settings")]
        public float destroyHoldTime = 1.0f;   // seconds to hold right-click
        private float destroyTimer = 0f;

        public LayerMask buildingLayer;        // assign your building layer here

        // ------------------------------------------------------------
        // INITIALIZATION
        // ------------------------------------------------------------
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
            input.Player.BuildMenu.performed += OnMenuPerformed;
            
            PlayerInventory.Instance.OnInventoryChanged += OnInventoryChanged;
        }

        private void OnDisable()
        {
            input.Player.Place.started -= OnPlaceStarted;
            input.Player.Place.canceled -= OnPlaceCanceled;
            input.Player.Cancel.performed -= OnCancelPerformed;
            input.Player.Rotate.performed -= OnRotatePerformed;
            input.Player.BuildMenu.performed -= OnMenuPerformed;
            
            PlayerInventory.Instance.OnInventoryChanged -= OnInventoryChanged;
        }

        // ------------------------------------------------------------
        // COST SYSTEM
        // ------------------------------------------------------------
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
        
        private void OnInventoryChanged()
        {
            if (!isPlacing || activePlacementLogic == null || selectedBuilding == null)
                return;

            UpdateAffordabilityVisuals();
        }
        
        private void UpdateAffordabilityVisuals()
        {
            int count = activePlacementLogic.GetPreviewCount();
            bool canAfford = CanAfford(selectedBuilding, count);

            Color tint = canAfford ? Color.green : Color.red;

            activePlacementLogic.SetGhostColor(tint);
            UIPlacementCostIndicator.Instance.SetColor(tint);

            // Keep the label updated in position and text
            activePlacementLogic.UpdateCostPreview(selectedBuilding);
        }

        // ------------------------------------------------------------
        // UPDATE LOOP
        // ------------------------------------------------------------
        private void Update()
        {
            // --------------------------------------------------------
            // PLACEMENT PREVIEW LOGIC
            // --------------------------------------------------------
            if (isPlacing && activePlacementLogic != null)
            {
                Vector3 snapped = SnapToGrid(GetMouseWorldPosition());

                if (isDragging)
                    activePlacementLogic.OnDragging(snapped);
                else
                    activePlacementLogic.UpdatePreview(snapped);
                
                // NEW — constantly update the cost preview
                activePlacementLogic.UpdateCostPreview(selectedBuilding);
                
                UpdateAffordabilityVisuals();

                return; // Do NOT allow destruction while placing
            }

            // --------------------------------------------------------
            // HOLD-TO-DESTROY LOGIC
            // --------------------------------------------------------
            HandleDestroyHold();
        }

        // ------------------------------------------------------------
        // ROTATION
        // ------------------------------------------------------------
        private void OnRotatePerformed(InputAction.CallbackContext ctx)
        {
            if (!isPlacing || activePlacementLogic == null)
                return;

            float scroll = ctx.ReadValue<float>();
            if (Mathf.Abs(scroll) < 0.01f)
                return;

            currentRotation += Mathf.Sign(scroll) * 90f;
            activePlacementLogic.ApplyRotation(currentRotation);
        }

        // ------------------------------------------------------------
        // START PLACEMENT
        // ------------------------------------------------------------
        public void StartPlacement(BuildingData building)
        {
            selectedBuilding = building;
            isPlacing = true;
            isDragging = false;
            currentRotation = 0f;

            activePlacementLogic = selectedBuilding.GetPlacementLogic();

            if (activePlacementLogic == null)
            {
                Debug.LogError($"No placement logic for {building.name}");
                return;
            }

            activePlacementLogic.Setup(building.prefab, currentRotation);

            InputContextManager.Instance.SetInputMode(InputContextManager.InputMode.Build);
            buildMenu.Hide();
        }

        // ------------------------------------------------------------
        // INPUT HANDLERS
        // ------------------------------------------------------------
        private void OnPlaceStarted(InputAction.CallbackContext ctx)
        {
            if (!isPlacing || selectedBuilding == null)
                return;

            Vector3 position = SnapToGrid(GetMouseWorldPosition());
            float dist = Vector2.Distance(position, player.position);

            if (dist > buildRange || !GridManager.Instance.GetClosestNode(position).walkable)
                return;

            isDragging = true;
            activePlacementLogic.OnStartDrag(position);
        }

        private void OnPlaceCanceled(InputAction.CallbackContext ctx)
        {
            if (!isPlacing || selectedBuilding == null)
                return;

            Vector3 pos = SnapToGrid(GetMouseWorldPosition());
            float dist = Vector2.Distance(pos, player.position);

            if (dist > buildRange || !GridManager.Instance.GetClosestNode(pos).walkable)
            {
                isDragging = false;
                activePlacementLogic.AbortDrag();
                return;
            }

            // Let placement logic update final drag state
            activePlacementLogic.OnEndDrag(pos);

            // VALIDATION
            if (!activePlacementLogic.ValidatePlacement(out object context))
            {
                Debug.Log("❌ Invalid placement by logic.");
                activePlacementLogic.AbortDrag();
                isDragging = false;
                return;
            }

            // GET ALL POSITIONS
            var positions = activePlacementLogic.GetPlacementPositions();
            int count = positions.Count;

            // COST CHECK
            if (!CanAfford(selectedBuilding, count))
            {
                Debug.Log("❌ Too expensive to place.");
                activePlacementLogic.AbortDrag();
                isDragging = false;
                return;
            }

            // PLACE ALL OBJECTS
            foreach (var placementPos in positions)
            {
                GameObject obj = Instantiate(
                    selectedBuilding.prefab,
                    placementPos,
                    Quaternion.Euler(0, 0, currentRotation)
                );

                // Assign BuildingData
                var placed = obj.GetComponent<PlacedBuilding>();
                if (placed)
                    placed.data = selectedBuilding;

                // Block nodes
                GridManager.Instance.BlockNodesUnderObject(obj);
            }

            // SPEND RESOURCES
            SpendCost(selectedBuilding, count);

            isDragging = false;

            // Reset for continuous placement
            activePlacementLogic.ClearPreview();
            activePlacementLogic.Setup(selectedBuilding.prefab, currentRotation);
        }

        private void OnCancelPerformed(InputAction.CallbackContext ctx)
        {
            if (!isPlacing)
                return;

            activePlacementLogic.Cancel();
            ExitBuildMode();
            buildMenu.Show();
        }

        private void OnMenuPerformed(InputAction.CallbackContext ctx)
        {
            if (InputContextManager.Instance.CurrentMode == InputContextManager.InputMode.Build)
            {
                if (isPlacing)
                    activePlacementLogic.Cancel();

                ExitBuildMode();
                buildMenu.Hide();
                InputContextManager.Instance.SetInputMode(InputContextManager.InputMode.Normal);
                return;
            }

            buildMenu.Show();
            InputContextManager.Instance.SetInputMode(InputContextManager.InputMode.Build);
        }

        private void ExitBuildMode()
        {
            selectedBuilding = null;
            isPlacing = false;
            isDragging = false;
            activePlacementLogic = null;
        }

        // ------------------------------------------------------------
        // HOLD-TO-DESTROY SYSTEM
        // ------------------------------------------------------------
        private void HandleDestroyHold()
        {
            // Only works in NORMAL mode
            if (InputContextManager.Instance.CurrentMode != InputContextManager.InputMode.Build)
            {
                destroyTimer = 0f;
                return;
            }

            // Detect right-click HOLD
            bool rightHeld = input.Player.Cancel.ReadValue<float>() > 0.5f;

            if (!rightHeld)
            {
                destroyTimer = 0f;
                return;
            }

            // Check if pointing at a building
            GameObject target = GetBuildingUnderCursor();
            if (target == null)
            {
                destroyTimer = 0f;
                return;
            }

            destroyTimer += Time.deltaTime;

            if (destroyTimer >= destroyHoldTime)
            {
                GridManager.Instance.UnblockNodesUnderObject(target);
                Destroy(target);
                destroyTimer = 0f;
            }
        }

        private GameObject GetBuildingUnderCursor()
        {
            Vector3 world = GetMouseWorldPosition();
            Collider2D hit = Physics2D.OverlapPoint(world, buildingLayer);
            return hit ? hit.gameObject : null;
        }

        // ------------------------------------------------------------
        // UTILS
        // ------------------------------------------------------------
        private Vector3 GetMouseWorldPosition()
        {
            Vector2 mouse = input.Player.Point.ReadValue<Vector2>();
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, -cam.transform.position.z));
            world.z = 0;
            return world;
        }

        private Vector3 SnapToGrid(Vector3 pos)
        {
            return GridManager.Instance.GetClosestNodeWorldPos(pos);
        }
    }
}
