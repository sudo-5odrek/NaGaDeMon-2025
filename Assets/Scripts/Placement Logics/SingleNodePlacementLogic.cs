using Building.Production;
using Grid;
using UnityEngine;

namespace Placement_Logics
{
    [CreateAssetMenu(menuName = "Build/Placement Logic/Single Node Only")]
    public class SingleNodePlacementLogic : ScriptableObject, IBuildPlacementLogic
    {
        private GameObject prefab;
        private float rotation;

        private GameObject previewObject;

        private System.Action<Vector3, GameObject> onPlacedCallback;

        [Header("Detection")]
        public LayerMask nodeMask;

        private static readonly Color VALID = new Color(0f, 1f, 0f, 0.35f);
        private static readonly Color INVALID = new Color(1f, 0f, 0f, 0.35f);

        // ----------------------------------------------------------------------
        // SETUP
        // ----------------------------------------------------------------------

        public void Setup(GameObject prefab, float rotation)
        {
            this.prefab = prefab;
            this.rotation = rotation;

            CreatePreview();
        }

        public void ApplyRotation(float newRotation)
        {
            rotation = newRotation;

            if (previewObject)
                previewObject.transform.rotation = Quaternion.Euler(0, 0, rotation);
        }

        public void SetPlacementCallback(System.Action<Vector3, GameObject> callback)
        {
            onPlacedCallback = callback;
        }

        // ----------------------------------------------------------------------
        // UPDATE PREVIEW (Hover)
        // ----------------------------------------------------------------------

        public void UpdatePreview(Vector3 worldCurrent)
        {
            if (!previewObject) return;

            var grid = GridManager.Instance;
            var (gx, gy) = grid.GridFromWorld(worldCurrent);

            Vector3 snapPos = grid.WorldFromGrid(gx, gy);
            previewObject.transform.position = snapPos;

            bool valid = GetNodeAt(gx, gy) != null;
            BuildUtils.SetPreviewTint(previewObject, valid ? VALID : INVALID);
        }

        // ----------------------------------------------------------------------
        // DRAG EVENTS
        // For single node placement: only OnEndDrag matters
        // ----------------------------------------------------------------------

        public void OnStartDrag(Vector3 start)
        {
            // Nothing needed for single placement
        }

        public void OnDragging(Vector3 current)
        {
            // Not used for single placement
        }

        public void OnEndDrag(Vector3 worldEnd)
        {
            TryPlace(worldEnd);
            ClearPreview();
        }

        // ----------------------------------------------------------------------
        // INTERNAL LOGIC
        // ----------------------------------------------------------------------

        private MiningNode GetNodeAt(int gx, int gy)
        {
            Vector3 worldPos = GridManager.Instance.WorldFromGrid(gx, gy);

            Collider2D col = Physics2D.OverlapPoint(worldPos, nodeMask);
            if (col != null && col.TryGetComponent(out MiningNode node))
                return node;

            return null;
        }

        private void TryPlace(Vector3 worldPos)
        {
            var grid = GridManager.Instance;
            var (gx, gy) = grid.GridFromWorld(worldPos);

            MiningNode node = GetNodeAt(gx, gy);

            if (node == null)
            {
                Debug.Log("❌ Cannot place: no mining node under cursor.");
                return;
            }

            Vector3 snapPos = grid.WorldFromGrid(gx, gy);

            // Instantiate the miner building
            GameObject obj = Object.Instantiate(prefab, snapPos, Quaternion.Euler(0, 0, rotation));

            // Block the node
            grid.BlockNodesUnderObject(obj);

            // Assign node to the miner
            if (obj.TryGetComponent(out MinerBuilding miner))
            {
                miner.AssignNode(node);
                Debug.Log($"🔗 Miner assigned to node: {node.name}");
            }

            // Disable the node collider — cannot collect manually anymore
            DisableNodeCollider(node);

            // Callback
            onPlacedCallback?.Invoke(snapPos, obj);
        }

        private void DisableNodeCollider(MiningNode node)
        {
            Collider2D col = node.GetComponent<Collider2D>();
            if (col)
            {
                col.enabled = false;
                Debug.Log($"🚫 Disabled collider on node: {node.name}");
            }
        }

        private void CreatePreview()
        {
            ClearPreview();

            previewObject = Object.Instantiate(prefab);
            BuildUtils.MakePreview(previewObject);
            BuildUtils.SetPreviewTint(previewObject, INVALID);
            previewObject.transform.rotation = Quaternion.Euler(0, 0, rotation);
        }

        public void ClearPreview()
        {
            if (previewObject)
                Object.Destroy(previewObject);

            previewObject = null;
        }

        public void Cancel()
        {
            ClearPreview();
        }
    }
}
