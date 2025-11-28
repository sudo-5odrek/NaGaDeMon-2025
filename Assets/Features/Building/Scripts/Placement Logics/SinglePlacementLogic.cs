using System.Collections.Generic;
using Features.Grid.Scripts;
using UnityEngine;

namespace Features.Building.Scripts.Placement_Logics
{
    [CreateAssetMenu(menuName = "Buildings/Placement Logic/Single")]
    public class SinglePlacementLogic : ScriptableObject, IBuildPlacementLogic
    {
        private GameObject prefab;
        private float rotation;

        private GameObject preview;
        private BoxCollider2D previewCollider;

        private LayerMask buildingMask;

        private Vector3 cachedSnapPosition;  

        private static readonly Color VALID   = new Color(0f, 1f, 0f, 0.35f);
        private static readonly Color INVALID = new Color(1f, 0f, 0f, 0.35f);

        // =====================================================================
        // SETUP
        // =====================================================================
        public void Setup(GameObject prefab, float rotation)
        {
            this.prefab = prefab;
            this.rotation = rotation;

            buildingMask = LayerMask.GetMask("Buildings");

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

            cachedSnapPosition = Snap(worldPos);
            preview.transform.position = cachedSnapPosition;

            BuildUtils.SetPreviewTint(preview, IsPlacementValid() ? VALID : INVALID);
        }

        public void OnStartDrag(Vector3 startPos) { }
        public void OnDragging(Vector3 currentPos) { }

        public void OnEndDrag(Vector3 endPos)
        {
            // No instantiation here — only update cached position
            cachedSnapPosition = Snap(endPos);
        }

        // =====================================================================
        // VALIDATION (IMPORTANT)
        // =====================================================================
        public bool ValidatePlacement(out object context)
        {
            bool valid = IsPlacementValid();
            context = null; // Single buildings have no special context
            return valid;
        }

        private bool IsPlacementValid()
        {
            if (previewCollider == null)
                return false;

            Collider2D hit = Physics2D.OverlapBox(
                previewCollider.bounds.center,
                previewCollider.bounds.size,
                preview.transform.eulerAngles.z,
                buildingMask
            );

            return hit == null;
        }

        public List<Vector3> GetPlacementPositions()
        {
            return new List<Vector3> { cachedSnapPosition };
        }

        // =====================================================================
        // HELPERS
        // =====================================================================
        private Vector3 Snap(Vector3 world)
        {
            var (gx, gy) = GridManager.Instance.GridFromWorld(world);
            return GridManager.Instance.WorldFromGrid(gx, gy);
        }

        private void CreatePreview()
        {
            ClearPreview();

            preview = Object.Instantiate(prefab);
            BuildUtils.MakePreview(preview);

            preview.transform.rotation = Quaternion.Euler(0, 0, rotation);

            // Copy collider
            BoxCollider2D src = prefab.GetComponentInChildren<BoxCollider2D>();
            if (src != null)
            {
                previewCollider = preview.AddComponent<BoxCollider2D>();
                previewCollider.size = src.size - new Vector2(0.05f, 0.05f);
                previewCollider.offset = src.offset;
                previewCollider.isTrigger = true;
            }

            preview.layer = LayerMask.NameToLayer("PlacementPreview");
        }

        public void ClearPreview()
        {
            if (preview)
                Object.Destroy(preview);

            preview = null;
            previewCollider = null;
            UIPlacementCostIndicator.Instance.Hide();
        }

        public void Cancel() => ClearPreview();

        public int GetPreviewCount() => 1;

        public Vector3 GetPreviewPlacement()
        {
            return preview ? preview.transform.position : Vector3.zero;
        }

        public void UpdateCostPreview(BuildingData data)
        {
            UIPlacementCostIndicator.Instance.ShowCost(
                data, 1, GetPreviewPlacement()
            );
        }

        public void SetGhostColor(Color color)
        {
            if (preview != null)
                BuildUtils.SetPreviewTint(preview, color);
        }

        public void AbortDrag()
        {
            
        }
    }
}
