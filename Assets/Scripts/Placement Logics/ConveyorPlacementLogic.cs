using System.Collections.Generic;
using Building.Conveyer;
using Grid;
using UnityEngine;

namespace Placement_Logics
{
    [CreateAssetMenu(menuName = "Build/Placement Logic/Conveyor Line")]
    public class ConveyorLinePlacementLogic : ScriptableObject, IBuildPlacementLogic
    {
        [Header("Visual Settings")]
        [SerializeField] private Color previewColor = new(1f, 1f, 1f, 0.4f);

        [Header("Prefabs")]
        [Tooltip("The prefab for the conveyor path (empty parent object that holds controller).")]
        public GameObject conveyorPathPrefab;

        private GameObject tilePrefab;
        private float rotation;
        private Vector3 worldStart;
        private bool isDragging = false;

        private System.Action<List<GameObject>> onPlaced;
        private GameObject hoverPreview;
        private readonly List<GameObject> previewLine = new();

        // NEW: persistent root for chaining
        private GameObject activePathRoot;
        private ConveyorPathController activeController;

        // --------------------------------------------------
        // CALLBACK + SETUP
        // --------------------------------------------------

        public void SetPlacementCallback(System.Action<List<GameObject>> callback)
        {
            onPlaced = callback;
        }

        public void Setup(GameObject prefab, float rotation, bool createPreview)
        {
            tilePrefab = prefab;
            this.rotation = rotation;

            if (createPreview)
                CreateHoverPreview();
        }

        public void BeginChain()
        {
            // create the conveyor path parent for this chain
            activePathRoot = Object.Instantiate(conveyorPathPrefab);
            activePathRoot.name = "ConveyorPath_Chain";
            activeController = activePathRoot.GetComponent<ConveyorPathController>();

            if (!activeController)
                Debug.LogError("[ConveyorLinePlacementLogic] Missing ConveyorPathController on prefab!");
        }

        public void EndChain()
        {
            activePathRoot = null;
            activeController = null;
        }

        // --------------------------------------------------
        // PREVIEW + PLACEMENT
        // --------------------------------------------------

        public void UpdatePreview(Vector3 worldCurrent)
        {
            if (!isDragging && hoverPreview)
                hoverPreview.transform.position = worldCurrent;
        }

        public void OnStartDrag(Vector3 start)
        {
            isDragging = true;
            worldStart = start;

            if (hoverPreview)
                Object.Destroy(hoverPreview);

            ClearPreviewLine();
        }

        public void OnDragging(Vector3 current)
        {
            if (!isDragging) return;
            ClearPreviewLine();
            DrawPreviewLine(worldStart, current);
        }

        public void OnEndDrag(Vector3 worldEnd)
        {
            if (!isDragging) return;
            isDragging = false;

            ClearPreviewLine();
            PlaceConveyorLine(worldStart, worldEnd);
        }

        // --------------------------------------------------
        // INTERNAL HELPERS
        // --------------------------------------------------

        private void ClearPreviewLine()
        {
            foreach (var g in previewLine)
                if (g) Object.Destroy(g);
            previewLine.Clear();
        }

        private void CreateHoverPreview()
        {
            hoverPreview = Object.Instantiate(tilePrefab);
            BuildUtils.MakePreview(hoverPreview);
        }

        private void DrawPreviewLine(Vector3 start, Vector3 end)
        {
            (int startX, int startY) = GridManager.Instance.GridFromWorld(start);
            (int endX, int endY) = GridManager.Instance.GridFromWorld(end);

            bool vertical = Mathf.Abs(endY - startY) > Mathf.Abs(endX - startX);
            if (vertical)
            {
                int step = startY < endY ? 1 : -1;
                for (int y = startY; y != endY + step; y += step)
                    previewLine.Add(CreateGhost(GridManager.Instance.WorldFromGrid(startX, y)));
            }
            else
            {
                int step = startX < endX ? 1 : -1;
                for (int x = startX; x != endX + step; x += step)
                    previewLine.Add(CreateGhost(GridManager.Instance.WorldFromGrid(x, startY)));
            }
        }

        private GameObject CreateGhost(Vector3 pos)
        {
            GameObject ghost = Object.Instantiate(tilePrefab, pos, Quaternion.Euler(0, 0, rotation));
            BuildUtils.MakePreview(ghost);
            return ghost;
        }

        // --------------------------------------------------
        // CONVEYOR LINE PLACEMENT
        // --------------------------------------------------

        private void PlaceConveyorLine(Vector3 start, Vector3 end)
        {
            if (tilePrefab == null)
            {
                Debug.LogError("[ConveyorLinePlacementLogic] tilePrefab is null!");
                return;
            }

            (int startX, int startY) = GridManager.Instance.GridFromWorld(start);
            (int endX, int endY) = GridManager.Instance.GridFromWorld(end);

            bool vertical = Mathf.Abs(endY - startY) > Mathf.Abs(endX - startX);
            List<GameObject> newTiles = new();

            if (activePathRoot == null)
                BeginChain(); // fallback — if chain somehow missing, make one

            Transform parent = activePathRoot.transform;

            if (vertical)
            {
                int step = startY < endY ? 1 : -1;
                for (int y = startY; y != endY + step; y += step)
                {
                    Vector3Int cell = new Vector3Int(startX, y, 0);
                    var tile = Object.Instantiate(tilePrefab, GridManager.Instance.WorldFromGrid(cell.x, cell.y), Quaternion.identity, parent);
                    newTiles.Add(tile);
                }
            }
            else
            {
                int step = startX < endX ? 1 : -1;
                for (int x = startX; x != endX + step; x += step)
                {
                    Vector3Int cell = new Vector3Int(x, startY, 0);
                    var tile = Object.Instantiate(tilePrefab, GridManager.Instance.WorldFromGrid(cell.x, cell.y), Quaternion.identity, parent);
                    newTiles.Add(tile);
                }
            }

            // Append to existing controller data
            activeController?.AddToPath(newTiles);

            // Notify manager
            onPlaced?.Invoke(newTiles);
        }

        public void ClearPreview()
        {
            ClearPreviewLine();
            if (hoverPreview)
                Object.Destroy(hoverPreview);
        }
    }
}
