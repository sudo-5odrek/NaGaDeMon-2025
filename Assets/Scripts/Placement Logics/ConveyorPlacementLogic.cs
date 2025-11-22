using System.Collections.Generic;
using Building;
using Grid;
using Interface;
using UnityEngine;

namespace Placement_Logics
{
    /// <summary>
    /// Handles visual previews and tile generation for conveyor placement.
    /// Used exclusively by ConnectionModeManager.
    /// Supports:
    /// - Hover preview
    /// - Live dragging preview line
    /// - Clean finalization/reset
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Buildings/Placement/Conveyor Line")]
    public class ConveyorLinePlacementLogic : ScriptableObject, IBuildPlacementLogic
    {
        private GameObject tilePrefab;
        private float rotation;

        private GameObject hoverPreview;
        private readonly List<GameObject> previewLine = new();

        private bool isDragging;
        private Vector3 dragStart;

        private System.Action<List<GameObject>> onPlaced;

        // ------------------------------------------------------
        // Setup (Called by ConnectionModeManager when mode opens)
        // ------------------------------------------------------
        public void Setup(GameObject prefab, float rotation)
        {
            tilePrefab = prefab;
            this.rotation = rotation;

            CreateHoverPreview();
        }

        public void ApplyRotation(float rotation) { /* Conveyor doesn't rotate manually */ }

        public void SetPlacementCallback(System.Action<List<GameObject>> callback)
        {
            onPlaced = callback;
        }

        // ------------------------------------------------------
        // Hover Preview (no dragging)
        // ------------------------------------------------------
        public void UpdatePreview(Vector3 worldPos)
        {
            if (!isDragging && hoverPreview)
                hoverPreview.transform.position = Snap(worldPos);
        }

        // ------------------------------------------------------
        // Dragging (visual line preview)
        // ------------------------------------------------------
        public void OnStartDrag(Vector3 startWorld)
        {
            isDragging = true;
            dragStart = Snap(startWorld);

            if (hoverPreview)
                Object.Destroy(hoverPreview);

            ClearPreviewLine();
        }

        public void OnDragging(Vector3 currentWorld)
        {
            if (!isDragging) return;

            ClearPreviewLine();
            DrawPreviewLine(dragStart, Snap(currentWorld));
        }

        public void OnEndDrag(Vector3 endWorld)
        {
            isDragging = false;
            ClearPreviewLine();

            // Recreate hover preview for next segment
            if (!hoverPreview)
                CreateHoverPreview();
        }

        // ------------------------------------------------------
        // Cleanup
        // ------------------------------------------------------
        public void ClearPreview()
        {
            ClearPreviewLine();

            if (hoverPreview)
                Object.Destroy(hoverPreview);

            hoverPreview = null;
            isDragging = false;
            UIPlacementCostIndicator.Instance.Hide();
        }

        public void Cancel() => ClearPreview();


        // ------------------------------------------------------
        // Used EXTERNALLY by ConnectionModeManager to actually place tiles
        // ------------------------------------------------------
        public List<GameObject> GenerateTiles(Vector3 startWorld, Vector3 endWorld, Transform parent)
        {
            Vector2Int a = GridToCell(startWorld);
            Vector2Int b = GridToCell(endWorld);

            bool vertical = Mathf.Abs(b.y - a.y) > Mathf.Abs(b.x - a.x);

            List<GameObject> tiles = new List<GameObject>();

            bool isContinuing = parent.childCount > 0;

            if (vertical)
            {
                int step = a.y <= b.y ? 1 : -1;
                int firstY = isContinuing ? a.y + step : a.y;

                for (int y = firstY; y != b.y + step; y += step)
                {
                    Vector3 pos = GridManager.Instance.WorldFromGrid(a.x, y);
                    tiles.Add(Object.Instantiate(tilePrefab, pos, Quaternion.identity, parent));
                }
            }
            else
            {
                int step = a.x <= b.x ? 1 : -1;
                int firstX = isContinuing ? a.x + step : a.x;

                for (int x = firstX; x != b.x + step; x += step)
                {
                    Vector3 pos = GridManager.Instance.WorldFromGrid(x, a.y);
                    tiles.Add(Object.Instantiate(tilePrefab, pos, Quaternion.identity, parent));
                }
            }

            onPlaced?.Invoke(tiles);
            return tiles;
        }


        // ------------------------------------------------------
        // INTERNAL GHOST LOGIC
        // ------------------------------------------------------
        private void CreateHoverPreview()
        {
            hoverPreview = Object.Instantiate(tilePrefab);
            BuildUtils.MakePreview(hoverPreview);
        }

        private void ClearPreviewLine()
        {
            foreach (var obj in previewLine)
                if (obj) Object.Destroy(obj);

            previewLine.Clear();
        }

        private void DrawPreviewLine(Vector3 start, Vector3 end)
        {
            Vector2Int a = GridToCell(start);
            Vector2Int b = GridToCell(end);

            bool vertical = Mathf.Abs(b.y - a.y) > Mathf.Abs(b.x - a.x);

            if (vertical)
            {
                int step = a.y <= b.y ? 1 : -1;

                for (int y = a.y; y != b.y + step; y += step)
                {
                    Vector3 pos = GridManager.Instance.WorldFromGrid(a.x, y);
                    previewLine.Add(CreateGhost(pos));
                }
            }
            else
            {
                int step = a.x <= b.x ? 1 : -1;

                for (int x = a.x; x != b.x + step; x += step)
                {
                    Vector3 pos = GridManager.Instance.WorldFromGrid(x, a.y);
                    previewLine.Add(CreateGhost(pos));
                }
            }
        }

        private GameObject CreateGhost(Vector3 pos)
        {
            var ghost = Object.Instantiate(tilePrefab, pos, Quaternion.identity);
            BuildUtils.MakePreview(ghost);
            return ghost;
        }


        // ------------------------------------------------------
        // UTILITY
        // ------------------------------------------------------
        private static Vector3 Snap(Vector3 world)
        {
            var (gx, gy) = GridManager.Instance.GridFromWorld(world);
            return GridManager.Instance.WorldFromGrid(gx, gy);
        }

        private static Vector2Int GridToCell(Vector3 world)
        {
            var (gx, gy) = GridManager.Instance.GridFromWorld(world);
            return new Vector2Int(gx, gy);
        }
        
        public int GetPreviewCount()
        {
            return Mathf.Max(previewLine.Count -1, 0);
        }
        
        
        public Vector3 GetPreviewPlacement()
        {
            // If no preview tiles, fallback to start position
            if (previewLine.Count == 0)
                return hoverPreview.transform.position;

            // The top-right object = the LAST ghost in the preview line
            GameObject last = previewLine[^1];
            return last ? last.transform.position : dragStart;
        }

        public void UpdateCostPreview(BuildingData data)
        {
            UIPlacementCostIndicator.Instance.ShowCost(
                data,
                GetPreviewCount(),
                GetPreviewPlacement()
            );
        }
        
        public void SetGhostColor(Color color)
        {
            // Hover tile (when not dragging)
            if (hoverPreview != null && !isDragging)
                BuildUtils.SetPreviewTint(hoverPreview, color);

            // All ghosts while dragging
            foreach (var g in previewLine)
            {
                if (g != null)
                    BuildUtils.SetPreviewTint(g, color);
            }
        }
        
        // ✅ NEW: drag ended with NO placement (too expensive / invalid / cancelled)
        public void AbortDrag()
        {
            if (!isDragging) return;

            isDragging = false;
            ClearPreviewLine();

            // recreate hover ghost so player can aim again
            if (hoverPreview == null)
                CreateHoverPreview();
        }

    }
}
