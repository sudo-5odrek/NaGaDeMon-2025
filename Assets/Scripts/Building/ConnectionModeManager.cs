using UnityEngine;
using UnityEngine.InputSystem;
using Grid;

namespace Building
{
    public class ConnectionModeManager : MonoBehaviour
    {
        public static ConnectionModeManager Instance;

        [Header("Settings")]
        public GameObject connectionPrefab;
        public ScriptableObject placementLogicAsset;
        public LayerMask buildingMask;

        [Header("References")]
        public Transform player;

        private IBuildPlacementLogic placementLogic;
        private bool isActive = false;
        private bool isDragging = false;

        private Camera cam;
        private InputSystem_Actions input;

        private BuildingConnector hoveredBuilding;
        private BuildingConnector startBuilding;

        // --------------------------------------------------
        // INITIALIZATION
        // --------------------------------------------------

        private void Awake()
        {
            Instance = this;
            cam = Camera.main;
            input = InputContextManager.Instance.input;

            if (placementLogicAsset is IBuildPlacementLogic logic)
                placementLogic = logic;
            else
                Debug.LogError($"[ConnectionModeManager] {placementLogicAsset} does not implement IBuildPlacementLogic!");
        }

        private void OnEnable()
        {
            input.Player.ConnectMode.performed += OnToggleMode;
            input.Player.Place.started += OnPlaceStarted;
            input.Player.Place.canceled += OnPlaceCanceled;
        }

        private void OnDisable()
        {
            input.Player.ConnectMode.performed -= OnToggleMode;
            input.Player.Place.started -= OnPlaceStarted;
            input.Player.Place.canceled -= OnPlaceCanceled;
        }

        // --------------------------------------------------
        // UPDATE LOOP
        // --------------------------------------------------

        private void Update()
        {
            if (!isActive) return;

            Vector3 mouseWorld = GetMouseWorldPosition();
            RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero, 0.1f, buildingMask);

            if (hit.collider && hit.collider.TryGetComponent(out BuildingConnector connector))
            {
                hoveredBuilding = connector;
                // Optional highlight
            }
            else
            {
                hoveredBuilding = null;
            }

            // ✅ Update preview position each frame (same as BuildManager)
            if (!isDragging)
                placementLogic?.UpdatePreview(mouseWorld);
            else
                placementLogic?.OnDragging(mouseWorld);
        }

        // --------------------------------------------------
        // MODE TOGGLING
        // --------------------------------------------------

        private void OnToggleMode(InputAction.CallbackContext ctx)
        {
            isActive = !isActive;

            if (isActive)
            {
                InputContextManager.Instance.SetInputMode(InputContextManager.InputMode.Build);
                //placementLogic?.Setup(connectionPrefab, 0f); // ✅ Create preview ghost
            }
            else
            {
                InputContextManager.Instance.SetInputMode(InputContextManager.InputMode.Normal);
                placementLogic?.ClearPreview();
                startBuilding = null;
                isDragging = false;
            }

            Debug.Log(isActive ? "🟢 Connection Mode ON" : "🔴 Connection Mode OFF");
        }

        // --------------------------------------------------
        // MOUSE INPUT
        // --------------------------------------------------

        private void OnPlaceStarted(InputAction.CallbackContext ctx)
        {
            if (!isActive || placementLogic == null)
                return;

            Vector3 mouseWorld = GetMouseWorldPosition();
            RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero, 0.1f, buildingMask);

            if (hit.collider && hit.collider.TryGetComponent(out BuildingConnector building))
            {
                if (building.CanProvideOutput())
                {
                    startBuilding = building;
                    isDragging = true;

                    if (placementLogic is IConnectionPlacementLogic connectLogic)
                        connectLogic.BeginFromBuilding(building, mouseWorld);
                    else
                        placementLogic.OnStartDrag(mouseWorld);
                }
            }
        }

        private void OnPlaceCanceled(InputAction.CallbackContext ctx)
        {
            if (!isActive || placementLogic == null)
                return;

            Vector3 mouseWorld = GetMouseWorldPosition();

            // ✅ End drag logic
            placementLogic.OnEndDrag(mouseWorld);
            isDragging = false;

            // ✅ Immediately recreate hover preview for next placement
            //placementLogic.Setup(connectionPrefab, 0f);

            startBuilding = null;
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
    }

    public interface IConnectionPlacementLogic : IBuildPlacementLogic
    {
        void BeginFromBuilding(BuildingConnector building, Vector3 worldStart);
    }
}
