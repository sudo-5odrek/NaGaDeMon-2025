using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Grid;
using Placement_Logics;

namespace Building
{
    public class ConnectionModeManager : MonoBehaviour
    {
        public static ConnectionModeManager Instance;

        [Header("Settings")]
        [Tooltip("The conveyor tile prefab used for placement.")]
        public GameObject connectionPrefab;

        [Tooltip("Placement logic ScriptableObject (e.g., ConveyorLinePlacementLogic).")]
        public ScriptableObject placementLogicAsset;

        [Tooltip("Which layers count as valid buildings for connection starts.")]
        public LayerMask buildingMask;

        [Header("References")]
        public Transform player;

        private IBuildPlacementLogic placementLogic;
        private Camera cam;
        private InputSystem_Actions input;

        private bool isActive;
        private bool isBuildingChain;

        private Vector3? startPoint;     // current segment start
        private Vector3? lastEndPoint;   // last segment end
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
            if (!isActive || !isBuildingChain || !startPoint.HasValue) return;

            // Continuous preview update
            Vector3 mouseWorld = GetMouseWorldPosition();
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
                placementLogic?.Setup(connectionPrefab, 0f, false);

                if (placementLogic is ConveyorLinePlacementLogic conveyorLogic)
                {
                    conveyorLogic.SetPlacementCallback(OnPlacementConfirmed);
                    conveyorLogic.BeginChain(); // ✅ Start parent path for this chain
                }

                Debug.Log("🟢 Connection Mode ON");
            }
            else
            {
                ExitConnectionMode();
            }
        }

        private void ExitConnectionMode()
        {
            InputContextManager.Instance.SetInputMode(InputContextManager.InputMode.Normal);

            if (placementLogic is ConveyorLinePlacementLogic conveyorLogic)
                conveyorLogic.EndChain(); // ✅ Clean up chain parent

            placementLogic?.ClearPreview();

            isActive = false;
            isBuildingChain = false;
            startPoint = null;
            lastEndPoint = null;
            startBuilding = null;

            Debug.Log("🔴 Connection Mode OFF");
        }

        // --------------------------------------------------
        // LEFT CLICK: PLACE / EXTEND
        // --------------------------------------------------

        private void OnLeftClick(InputAction.CallbackContext ctx)
        {
            if (!isActive) return;

            Vector3 mouseWorld = GetMouseWorldPosition();

            // STEP 1 — Start chain from a building
            if (!isBuildingChain)
            {
                RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero, 0.1f, buildingMask);
                if (hit.collider && hit.collider.TryGetComponent(out BuildingConnector connector))
                {
                    if (connector.CanProvideOutput())
                    {
                        startBuilding = connector;
                        startPoint = connector.transform.position;
                        placementLogic.OnStartDrag(startPoint.Value);
                        isBuildingChain = true;
                        Debug.Log($"🟩 Started conveyor chain from {connector.name}");
                    }
                }
                return;
            }

            // STEP 2 — Already chaining: confirm this segment
            if (isBuildingChain && startPoint.HasValue)
            {
                Vector3 endPoint = SnapToGrid(mouseWorld);

                placementLogic.OnEndDrag(endPoint); // place tiles between start and end
                lastEndPoint = endPoint;
                startPoint = endPoint;

                // Start a new preview immediately for the next segment
                placementLogic.OnStartDrag(startPoint.Value);
            }
        }

        // --------------------------------------------------
        // RIGHT CLICK: CANCEL CHAIN
        // --------------------------------------------------

        private void OnRightClick(InputAction.CallbackContext ctx)
        {
            if (!isActive) return;

            Debug.Log("🛑 Conveyor chain canceled by right-click.");
            ExitConnectionMode();
        }

        // --------------------------------------------------
        // CALLBACK FROM PLACEMENT LOGIC
        // --------------------------------------------------

        private void OnPlacementConfirmed(List<GameObject> placedObjects)
        {
            if (placedObjects == null || placedObjects.Count < 2)
                return;

            // Determine direction from first two tiles
            Vector3 p1 = placedObjects[0].transform.position;
            Vector3 p2 = placedObjects[1].transform.position;

            Vector2Int dir = Vector2Int.zero;
            float dx = p2.x - p1.x;
            float dy = p2.y - p1.y;

            if (Mathf.Abs(dx) > Mathf.Abs(dy))
                dir = dx > 0 ? Vector2Int.right : Vector2Int.left;
            else
                dir = dy > 0 ? Vector2Int.up : Vector2Int.down;

            float angle = GetRotationFromDirection(dir);

            foreach (var obj in placedObjects)
                obj.transform.rotation = Quaternion.Euler(0, 0, angle);

            lastEndPoint = placedObjects[^1].transform.position;
            Debug.Log($"✅ Segment placed. Direction: {dir}");
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

        private float GetRotationFromDirection(Vector2Int dir)
        {
            if (dir == Vector2Int.up) return 0f;
            if (dir == Vector2Int.right) return -90f;
            if (dir == Vector2Int.down) return 180f;
            if (dir == Vector2Int.left) return 90f;
            return 0f;
        }
    }
}
