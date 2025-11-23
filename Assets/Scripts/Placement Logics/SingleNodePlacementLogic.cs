using Building;
using Building.Production;
using Grid;
using Interface;
using UnityEngine;
using System.Collections.Generic;

namespace Placement_Logics
{
    [CreateAssetMenu(menuName = "Game/Buildings/Placement/Single Node Only")]
    public class SingleNodePlacementLogic : ScriptableObject, IBuildPlacementLogic
    {
        private GameObject prefab;
        private float rotation;

        private GameObject preview;
        private MiningNode hoveredNode;

        private LayerMask nodeMask;

        private Vector3 cachedPlacementPos;
        private MiningNode cachedPlacementNode;

        private static readonly Color VALID   = new Color(0f, 1f, 0f, 0.35f);
        private static readonly Color INVALID = new Color(1f, 0f, 0f, 0.35f);

        // =====================================================================
        // SETUP
        // =====================================================================
        public void Setup(GameObject prefab, float rotation)
        {
            this.prefab = prefab;
            this.rotation = rotation;

            nodeMask = LayerMask.GetMask("Node");

            CreatePreview();
        }

        public void ApplyRotation(float newRotation)
        {
            rotation = newRotation;
            if (preview)
                preview.transform.rotation = Quaternion.Euler(0, 0, rotation);
        }

        // =====================================================================
        // PREVIEW UPDATE
        // =====================================================================
        public void UpdatePreview(Vector3 worldPos)
        {
            if (!preview) return;

            var grid = GridManager.Instance;

            var (gx, gy) = grid.GridFromWorld(worldPos);
            Vector3 snapPos = grid.WorldFromGrid(gx, gy);

            preview.transform.position = snapPos;

            hoveredNode = GetNodeAt(gx, gy);

            BuildUtils.SetPreviewTint(preview, hoveredNode ? VALID : INVALID);
        }

        // These two are unused but required
        public void OnStartDrag(Vector3 startPos) { }
        public void OnDragging(Vector3 currentPos) { }

        public void OnEndDrag(Vector3 endPos)
        {
            // Cache final snapped position
            cachedPlacementPos = GetPreviewPlacement();
        }

        public void AbortDrag()
        {
            cachedPlacementPos = Vector3.zero;
            cachedPlacementNode = null;

            ClearPreview();
            CreatePreview();
        }

        public void Cancel() => ClearPreview();

        // =====================================================================
        // VALIDATION (Key logic)
        // =====================================================================
        public bool ValidatePlacement(out object context)
        {
            if (!preview)
            {
                context = null;
                return false;
            }

            var grid = GridManager.Instance;
            Vector3 pos = preview.transform.position;

            var (gx, gy) = grid.GridFromWorld(pos);

            MiningNode node = GetNodeAt(gx, gy);
            if (node == null)
            {
                cachedPlacementNode = null;
                cachedPlacementPos = Vector3.zero;
                context = null;
                return false;
            }

            cachedPlacementNode = node;
            cachedPlacementPos = grid.WorldFromGrid(gx, gy);

            // Pass the MiningNode to the MinerBuilding later
            context = node;
            return true;
        }

        // =====================================================================
        // REQUIRED API FOR BuildManager
        // =====================================================================
        public List<Vector3> GetPlacementPositions()
        {
            return new List<Vector3> { cachedPlacementPos };
        }

        public int GetPreviewCount() => 1;

        public Vector3 GetPreviewPlacement()
        {
            return preview ? preview.transform.position : Vector3.zero;
        }

        public void UpdateCostPreview(BuildingData data)
        {
            UIPlacementCostIndicator.Instance.ShowCost(
                data,
                1,
                GetPreviewPlacement()
            );
        }

        public void SetGhostColor(Color color)
        {
            if (preview)
                BuildUtils.SetPreviewTint(preview, color);
        }

        // =====================================================================
        // HELPERS
        // =====================================================================
        private MiningNode GetNodeAt(int gx, int gy)
        {
            Vector3 worldPos = GridManager.Instance.WorldFromGrid(gx, gy);

            Collider2D col = Physics2D.OverlapPoint(worldPos, nodeMask);
            if (col != null && col.TryGetComponent(out MiningNode node))
                return node;

            return null;
        }

        private void CreatePreview()
        {
            ClearPreview();

            preview = Object.Instantiate(prefab);
            BuildUtils.MakePreview(preview);
            BuildUtils.SetPreviewTint(preview, INVALID);

            preview.transform.rotation = Quaternion.Euler(0, 0, rotation);
        }

        public void ClearPreview()
        {
            if (preview)
                Object.Destroy(preview);

            preview = null;
            UIPlacementCostIndicator.Instance.Hide();
        }
    }
}
