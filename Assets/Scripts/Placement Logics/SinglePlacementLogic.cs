using Building;
using Grid;
using Interface;
using UnityEngine;

namespace Placement_Logics
{
    [CreateAssetMenu(menuName = "Game/Buildings/Placement/Single")]
    public class SinglePlacementLogic : ScriptableObject, IBuildPlacementLogic
    {
        private GameObject prefab;
        private float rotation;

        private GameObject preview;
        private BoxCollider2D previewCollider;

        // Must match your "Buildings" physics layer
        private LayerMask buildingMask;

        private static readonly Color VALID   = new Color(0f, 1f, 0f, 0.35f);
        private static readonly Color INVALID = new Color(1f, 0f, 0f, 0.35f);

        // ----------------------------------------------------------------------
        // SETUP
        // ----------------------------------------------------------------------
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

        // ----------------------------------------------------------------------
        // PREVIEW UPDATE
        // ----------------------------------------------------------------------
        public void UpdatePreview(Vector3 worldPos)
        {
            if (!preview) return;

            Vector3 snap = Snap(worldPos);
            preview.transform.position = snap;

            bool valid = !IsOverlappingBuilding();

            BuildUtils.SetPreviewTint(preview, valid ? VALID : INVALID);
        }

        // ----------------------------------------------------------------------
        // DRAG EVENTS
        // ----------------------------------------------------------------------
        public void OnStartDrag(Vector3 start) { }
        public void OnDragging(Vector3 current) { }

        public void OnEndDrag(Vector3 worldEnd)
        {
            Vector3 snap = Snap(worldEnd);

            if (IsOverlappingBuilding())
            {
                Debug.Log("❌ Cannot place: overlaps an existing building.");
                return;
            }

            Place(snap);
            ClearPreview();
        }

        public void Cancel() => ClearPreview();

        public void ClearPreview()
        {
            if (preview)
                Object.Destroy(preview);
            
            preview = null;
            previewCollider = null;
            UIPlacementCostIndicator.Instance.Hide();
        }

        // ----------------------------------------------------------------------
        // INTERNAL HELPERS
        // ----------------------------------------------------------------------
        private void CreatePreview()
        {
            ClearPreview();

            // Instantiate the preview version of the building
            preview = Object.Instantiate(prefab);
            BuildUtils.MakePreview(preview);

            preview.transform.rotation = Quaternion.Euler(0, 0, rotation);

            // Copy the BoxCollider2D from the prefab
            BoxCollider2D src = prefab.GetComponentInChildren<BoxCollider2D>();
            if (src != null)
            {
                previewCollider = preview.AddComponent<BoxCollider2D>();
                previewCollider.size = src.size;
                previewCollider.offset = src.offset;
                previewCollider.isTrigger = true;

                // 🔥 Slight collider shrink to avoid false positive overlaps
                Vector2 shrink = new Vector2(0.05f, 0.05f);
                previewCollider.size -= shrink;
            }
            else
            {
                Debug.LogWarning("⚠ No BoxCollider2D found on the prefab! Single placement requires one.");
            }

            preview.layer = LayerMask.NameToLayer("PlacementPreview");
        }

        private bool IsOverlappingBuilding()
        {
            if (!previewCollider) return true;

            Collider2D hit = Physics2D.OverlapBox(
                previewCollider.bounds.center,
                previewCollider.bounds.size,
                preview.transform.eulerAngles.z,
                buildingMask
            );

            return hit != null;
        }

        private void Place(Vector3 pos)
        {
            GameObject obj = Object.Instantiate(prefab, pos, Quaternion.Euler(0, 0, rotation));
            GridManager.Instance.BlockNodesUnderObject(obj);
        }

        private Vector3 Snap(Vector3 world)
        {
            var (gx, gy) = GridManager.Instance.GridFromWorld(world);
            return GridManager.Instance.WorldFromGrid(gx, gy);
        }
        
        public int GetPreviewCount()
        {
            return 1;
        }
        
        public Vector3 GetPreviewPlacement()
        {
            return preview ? preview.transform.position : Vector3.zero;
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
            if (preview != null)
                BuildUtils.SetPreviewTint(preview, color);
        }

    }
}
