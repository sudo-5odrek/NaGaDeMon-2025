using Grid;
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
        }

        private void OnDisable()
        {
            input.Player.Place.started -= OnPlaceStarted;
            input.Player.Place.canceled -= OnPlaceCanceled;
            input.Player.Cancel.performed -= OnCancelPerformed;
            input.Player.Rotate.performed -= OnRotatePerformed;
            input.Player.BuildMenu.performed -= OnMenuPerformed;
        }

        // -------------------------------------------------------------------
        // COST SYSTEM (Only thing BuildManager truly owns)
        // -------------------------------------------------------------------
        private bool CanAfford(BuildingData building)
        {
            if (!building) return false;

            var inv = PlayerInventory.Instance;
            foreach (var c in building.cost)
                if (inv.GetAmount(c.item) < c.amount)
                    return false;

            return true;
        }

        private void SpendCost(BuildingData building)
        {
            var inv = PlayerInventory.Instance;
            foreach (var c in building.cost)
                inv.RemoveItem(c.item, c.amount);
        }

        // -------------------------------------------------------------------
        // MAIN UPDATE LOOP
        // -------------------------------------------------------------------
        private void Update()
        {
            if (!isPlacing || activePlacementLogic == null)
                return;

            Vector3 snapped = SnapToGrid(GetMouseWorldPosition());

            if (isDragging)
                activePlacementLogic.OnDragging(snapped);
            else
                activePlacementLogic.UpdatePreview(snapped);
        }

        // -------------------------------------------------------------------
        // ROTATION (BuildManager only forwards the rotation)
        // -------------------------------------------------------------------
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

        // -------------------------------------------------------------------
        // START PLACEMENT
        // -------------------------------------------------------------------
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

        // -------------------------------------------------------------------
        // INPUT HANDLERS
        // -------------------------------------------------------------------
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

            // End placement attempt
            Vector3 pos = SnapToGrid(GetMouseWorldPosition());
            float dist = Vector2.Distance(pos, player.position);

            if (dist > buildRange || !GridManager.Instance.GetClosestNode(pos).walkable)
            {
                isDragging = false;
                return;
            }

            // VALID placement → check cost
            if (!CanAfford(selectedBuilding))
            {
                Debug.Log("Cannot afford!");
                isDragging = false;
                return;
            }

            // place
            activePlacementLogic.OnEndDrag(pos);
            SpendCost(selectedBuilding);

            isDragging = false;

            // Immediately recreate preview so player can place again
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

        // -------------------------------------------------------------------
        // UTILS
        // -------------------------------------------------------------------
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
