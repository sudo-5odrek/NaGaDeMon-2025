using System.Collections.Generic;
using Grid;
using UnityEngine;

namespace Placement_Logics
{
    /// <summary>
    /// Handles visual previews and tile generation for conveyor placement.
    /// Supports live hover preview, line preview while dragging, and clean ghost cleanup.
    /// </summary>
    [CreateAssetMenu(menuName = "Build/Placement Logic/Conveyor Line")]
    public class ConveyorLinePlacementLogic : ScriptableObject, IBuildPlacementLogic
    {
        [Header("Visual Settings")]
        [SerializeField] private Color previewColor = new(1f, 1f, 1f, 0.4f);

        private GameObject tilePrefab;
        private float rotation;

        // --- Preview state ---
        private GameObject hoverPreview;
        private readonly List<GameObject> previewLine = new();
        private bool isDragging;
        private Vector3 anchorStart;

        private System.Action<List<GameObject>> onPlaced;

        // --------------------------------------------------
        // SETUP
        // --------------------------------------------------
        public void Setup(GameObject prefab, float rotation, bool createPreview)
        {
            tilePrefab = prefab;
            this.rotation = rotation;

            if (createPreview)
                CreateHoverPreview();
        }
        public void FinalizePath()
        {
            // End any drag state and remove all preview ghosts
            isDragging = false;
            ClearPreviewLine();

            if (hoverPreview)
            {
                Object.Destroy(hoverPreview);
                hoverPreview = null;
            }
        }
        public void SetPlacementCallback(System.Action<List<GameObject>> callback)
        {
            onPlaced = callback;
        }
        
        // --------------------------------------------------
        // PREVIEW LIFECYCLE
        // --------------------------------------------------
        public void UpdatePreview(Vector3 worldCurrent)
        {
            // when not dragging, follow mouse with a hover ghost
            if (!isDragging && hoverPreview != null)
                hoverPreview.transform.position = SnapWorld(worldCurrent);
        }

        public void OnStartDrag(Vector3 start)
        {
            isDragging = true;
            anchorStart = SnapWorld(start);

            if (hoverPreview)
                Object.Destroy(hoverPreview);

            ClearPreviewLine();
        }

        public void OnDragging(Vector3 current)
        {
            if (!isDragging) return;
            ClearPreviewLine();
            DrawPreviewLine(anchorStart, SnapWorld(current));
        }

        public void OnEndDrag(Vector3 worldEnd)
        {
            isDragging = false;
            ClearPreviewLine();

            // restore hover preview
            if (tilePrefab != null && hoverPreview == null)
                CreateHoverPreview();
        }

        public void ClearPreview()
        {
            ClearPreviewLine();
            if (hoverPreview) Object.Destroy(hoverPreview);
            hoverPreview = null;
            isDragging = false;
        }

        // --------------------------------------------------
        // TILE GENERATION (used by ConnectionModeManager)
        // --------------------------------------------------
        public List<GameObject> GenerateTiles(Vector3 start, Vector3 end, Transform parent)
        {
            var a = GridToCell(start);
            var b = GridToCell(end);

            bool vertical = Mathf.Abs(b.y - a.y) > Mathf.Abs(b.x - a.x);
            var tiles = new List<GameObject>();

            // ✅ Determine if this is a continuation of a chain
            bool isContinuingChain = parent.childCount > 0;

            if (vertical)
            {
                int step = a.y <= b.y ? 1 : -1;
                int startY = isContinuingChain ? a.y + step : a.y; // ✅ start one step after if continuing
                for (int y = startY; y != b.y + step; y += step)
                {
                    var pos = GridManager.Instance.WorldFromGrid(a.x, y);
                    tiles.Add(Object.Instantiate(tilePrefab, pos, Quaternion.identity, parent));
                }
            }
            else
            {
                int step = a.x <= b.x ? 1 : -1;
                int startX = isContinuingChain ? a.x + step : a.x; // ✅ start one step after if continuing
                for (int x = startX; x != b.x + step; x += step)
                {
                    var pos = GridManager.Instance.WorldFromGrid(x, a.y);
                    tiles.Add(Object.Instantiate(tilePrefab, pos, Quaternion.identity, parent));
                }
            }

            onPlaced?.Invoke(tiles);
            return tiles;
        }


        // --------------------------------------------------
        // INTERNAL HELPERS
        // --------------------------------------------------
        private void CreateHoverPreview()
        {
            hoverPreview = Object.Instantiate(tilePrefab);
            BuildUtils.MakePreview(hoverPreview);
        }

        private void ClearPreviewLine()
        {
            foreach (var g in previewLine)
                if (g) Object.Destroy(g);
            previewLine.Clear();
        }

        private void DrawPreviewLine(Vector3 start, Vector3 end)
        {
            var a = GridToCell(start);
            var b = GridToCell(end);
            bool vertical = Mathf.Abs(b.y - a.y) > Mathf.Abs(b.x - a.x);

            if (vertical)
            {
                int step = a.y <= b.y ? 1 : -1;
                for (int y = a.y; y != b.y + step; y += step)
                    previewLine.Add(CreateGhost(GridManager.Instance.WorldFromGrid(a.x, y)));
            }
            else
            {
                int step = a.x <= b.x ? 1 : -1;
                for (int x = a.x; x != b.x + step; x += step)
                    previewLine.Add(CreateGhost(GridManager.Instance.WorldFromGrid(x, a.y)));
            }
        }

        private GameObject CreateGhost(Vector3 pos)
        {
            var ghost = Object.Instantiate(tilePrefab, pos, Quaternion.Euler(0, 0, rotation));
            BuildUtils.MakePreview(ghost);
            return ghost;
        }

        private static Vector3 SnapWorld(Vector3 world)
        {
            (int gx, int gy) = GridManager.Instance.GridFromWorld(world);
            return GridManager.Instance.WorldFromGrid(gx, gy);
        }

        private static Vector2Int GridToCell(Vector3 world)
        {
            (int gx, int gy) = GridManager.Instance.GridFromWorld(world);
            return new Vector2Int(gx, gy);
        }
    }
}
