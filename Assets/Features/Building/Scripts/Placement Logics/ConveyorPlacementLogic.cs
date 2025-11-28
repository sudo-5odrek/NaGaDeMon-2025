using System.Collections.Generic;
using Building;
using Grid;
using Interface;
using UnityEngine;

namespace Placement_Logics
{
    [CreateAssetMenu(menuName = "Game/Buildings/Placement/Conveyor Line")]
    public class ConveyorLinePlacementLogic : ScriptableObject, IBuildPlacementLogic
    {
        private GameObject tilePrefab;
        private float rotation;

        private GameObject hoverPreview;
        private readonly List<GameObject> previewLine = new();

        private bool isDragging;
        private Vector3 dragStart;

        // =====================================================================
        // SETUP
        // =====================================================================
        public void Setup(GameObject prefab, float rotation)
        {
            tilePrefab = prefab;
            this.rotation = rotation;

            ClearPreview();
            CreateHoverPreview();
        }

        public void ApplyRotation(float r)
        {
            // Conveyor tiles don't rotate via scroll in your setup,
            // but you can enable this if needed.
        }

        // =====================================================================
        // PREVIEW UPDATE (idle hover)
        // =====================================================================
        public void UpdatePreview(Vector3 worldPos)
        {
            if (!isDragging && hoverPreview)
                hoverPreview.transform.position = Snap(worldPos);
        }

        // =====================================================================
        // DRAG EVENTS
        // =====================================================================
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
            if (!isDragging) return;

            // We STOP dragging but KEEP the preview line
            // so ConnectionModeManager can inspect it.
            isDragging = false;
            ClearPreviewLine();
        }

        public void AbortDrag()
        {
            isDragging = false;
            ClearPreviewLine();

            if (hoverPreview == null && tilePrefab != null)
                CreateHoverPreview();
        }

        public void Cancel()
        {
            isDragging = false;
            ClearPreview();
        }

        // =====================================================================
        // VALIDATION & POSITIONS
        // =====================================================================
        public bool ValidatePlacement(out object context)
        {
            context = null;

            int count = previewLine.Count;
            if (count == 0)
                return false;

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = previewLine[i].transform.position;

                // ----------------------------------------------------
                // SKIP FIRST AND LAST TILE VALIDATION
                // ----------------------------------------------------
                if (i == 0 || i == count - 1)
                    continue;

                // ----------------------------------------------------
                // REGULAR VALIDATION FOR ALL MIDDLE TILES
                // ----------------------------------------------------
                var node = GridManager.Instance.GetClosestNode(pos);
                if (node == null || !node.walkable)
                    return false;
            }

            return true;
        }

        public List<Vector3> GetPlacementPositions()
        {
            var list = new List<Vector3>(previewLine.Count);

            foreach (var g in previewLine)
            {
                if (g)
                    list.Add(g.transform.position);
            }

            return list;
        }

        public int GetPreviewCount()
        {
            // While dragging: number of ghost tiles
            if (isDragging)
                return previewLine.Count;

            // Not dragging: 1 hover tile if present
            return hoverPreview ? 1 : 0;
        }

        public Vector3 GetPreviewPlacement()
        {
            if (isDragging && previewLine.Count > 0)
                return previewLine[^1].transform.position;

            if (!isDragging && hoverPreview)
                return hoverPreview.transform.position;

            return dragStart;
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
            if (!isDragging && hoverPreview != null)
                BuildUtils.SetPreviewTint(hoverPreview, color);

            foreach (var g in previewLine)
            {
                if (g != null)
                    BuildUtils.SetPreviewTint(g, color);
            }
        }

        // =====================================================================
        // INTERNAL GHOST SYSTEM
        // =====================================================================
        public void ClearPreview()
        {
            ClearPreviewLine();

            if (hoverPreview)
                Object.Destroy(hoverPreview);

            hoverPreview = null;
            UIPlacementCostIndicator.Instance.Hide();
        }

        private void CreateHoverPreview()
        {
            if (!tilePrefab) return;

            hoverPreview = Object.Instantiate(tilePrefab);
            BuildUtils.MakePreview(hoverPreview);
        }

        private void ClearPreviewLine()
        {
            foreach (var g in previewLine)
            {
                if (g)
                    Object.Destroy(g);
            }

            previewLine.Clear();
        }

        private void DrawPreviewLine(Vector3 start, Vector3 end)
        {
            Vector2Int a = SnapToCell(start);
            Vector2Int b = SnapToCell(end);

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
            GameObject ghost = Object.Instantiate(tilePrefab, pos, Quaternion.identity);
            BuildUtils.MakePreview(ghost);
            return ghost;
        }

        // =====================================================================
        // UTILITY
        // =====================================================================
        private static Vector3 Snap(Vector3 world)
        {
            var (gx, gy) = GridManager.Instance.GridFromWorld(world);
            return GridManager.Instance.WorldFromGrid(gx, gy);
        }

        private static Vector2Int SnapToCell(Vector3 world)
        {
            var (gx, gy) = GridManager.Instance.GridFromWorld(world);
            return new Vector2Int(gx, gy);
        }
    }
}
