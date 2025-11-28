using System.Collections.Generic;
using Features.Grid.Scripts;
using UnityEngine;

namespace Features.Building.Scripts.Placement_Logics
{
    [CreateAssetMenu(menuName = "Buildings/Placement Logic/Straight Line")]
    public class StraightLinePlacementLogic : ScriptableObject, IBuildPlacementLogic
    {
        private GameObject prefab;
        private float rotation;

        private Vector3 worldStart;
        private bool isDragging = false;

        private GameObject hoverPreview;                 // ghost before dragging
        private readonly List<GameObject> previewLine = new(); // ghosts during drag

        private readonly List<Vector3> cachedLinePositions = new(); // returned to BuildManager

        // Optional callback (you usually do NOT need it anymore)
        private System.Action<List<Vector3>> onPlaced;

        public void SetPlacementCallback(System.Action<List<Vector3>> callback)
        {
            onPlaced = callback;
        }

        // =====================================================================
        // SETUP
        // =====================================================================
        public void Setup(GameObject prefab, float rotation)
        {
            this.prefab = prefab;
            this.rotation = rotation;

            CreateHoverPreview();
        }

        public void ApplyRotation(float newRotation)
        {
            rotation = newRotation;

            if (hoverPreview)
                hoverPreview.transform.rotation = Quaternion.Euler(0, 0, rotation);

            foreach (var g in previewLine)
                if (g) g.transform.rotation = Quaternion.Euler(0, 0, rotation);
        }

        // =====================================================================
        // PREVIEW UPDATE
        // =====================================================================
        public void UpdatePreview(Vector3 worldCurrent)
        {
            if (!isDragging && hoverPreview)
                hoverPreview.transform.position = worldCurrent;
        }

        // =====================================================================
        // DRAG EVENTS
        // =====================================================================
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
            
            cachedLinePositions.Clear();
            foreach (var g in previewLine)
            {
                if (g)
                    cachedLinePositions.Add(g.transform.position);
            }
        }

        public void OnEndDrag(Vector3 endPos)
        {
            if (!isDragging) return;

            isDragging = false;

            // Cache all placement positions for BuildManager
            cachedLinePositions.Clear();
            foreach (var ghost in previewLine)
            {
                if (ghost)
                    cachedLinePositions.Add(ghost.transform.position);
            }

            ClearPreviewLine();
            CreateHoverPreview(); // return to idle hover mode
        }

        public void AbortDrag()
        {
            if (!isDragging) return;

            isDragging = false;

            cachedLinePositions.Clear();
            ClearPreviewLine();

            if (!hoverPreview)
                CreateHoverPreview();
        }

        // =====================================================================
        // VALIDATION
        // =====================================================================
        public bool ValidatePlacement(out object context)
        {
            context = null;

            // If we have NO tiles, nothing to place
            if (cachedLinePositions.Count == 0)
                return false;

            // Validate every tile
            foreach (Vector3 pos in cachedLinePositions)
            {
                var node = GridManager.Instance.GetClosestNode(pos);
                if (node == null || !node.walkable)
                    return false;
            }

            return true;
        }

        // =====================================================================
        // REQUIRED API FOR BuildManager
        // =====================================================================
        public List<Vector3> GetPlacementPositions()
        {
            return new List<Vector3>(cachedLinePositions);
        }

        public int GetPreviewCount()
        {
            if (isDragging)
                return cachedLinePositions.Count;

            return hoverPreview ? 1 : 0;
        }

        public Vector3 GetPreviewPlacement()
        {
            if (isDragging && cachedLinePositions.Count > 0)
                return cachedLinePositions[cachedLinePositions.Count - 1];

            if (!isDragging && hoverPreview)
                return hoverPreview.transform.position;

            return worldStart;
        }

        public void UpdateCostPreview(BuildingData data)
        {
            int count = GetPreviewCount();
            Vector3 pos = GetPreviewPlacement();

            UIPlacementCostIndicator.Instance.ShowCost(data, count, pos);
        }

        public void SetGhostColor(Color color)
        {
            if (!isDragging && hoverPreview)
                BuildUtils.SetPreviewTint(hoverPreview, color);

            foreach (var g in previewLine)
                if (g != null)
                    BuildUtils.SetPreviewTint(g, color);
        }

        public void Cancel()
        {
            isDragging = false;
            ClearPreview();
        }

        public void ClearPreview()
        {
            ClearPreviewLine();

            if (hoverPreview)
                Object.Destroy(hoverPreview);

            hoverPreview = null;
            UIPlacementCostIndicator.Instance.Hide();
        }

        // =====================================================================
        // INTERNAL HELPERS
        // =====================================================================
        private void CreateHoverPreview()
        {
            ClearPreviewLine();

            if (!prefab) return;

            hoverPreview = Object.Instantiate(prefab);
            BuildUtils.MakePreview(hoverPreview);
            hoverPreview.transform.rotation = Quaternion.Euler(0, 0, rotation);
        }

        private void ClearPreviewLine()
        {
            foreach (var g in previewLine)
                if (g) Object.Destroy(g);

            previewLine.Clear();
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
            GameObject ghost = Object.Instantiate(prefab, pos, Quaternion.Euler(0, 0, rotation));
            BuildUtils.MakePreview(ghost);
            return ghost;
        }
    }
}
