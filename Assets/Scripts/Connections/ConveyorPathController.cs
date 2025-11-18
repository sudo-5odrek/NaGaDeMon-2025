using System.Collections.Generic;
using Inventory;
using UnityEngine;

namespace Building.Conveyer
{
    [DisallowMultipleComponent]
    public class ConveyorPathController : MonoBehaviour
    {
        [Header("Path Configuration")]
        public List<GameObject> pathTiles = new();
        public float speed = 2f;
        public float spawnDelay = 0.5f;

        [Header("Connections")]
        public BuildingInventory startInventory;
        public BuildingInventory endInventory;
        public string startPortName;

        [Header("Runtime Settings")]
        public bool isBlocked;

        private readonly List<MovingItem> activeItems = new();
        private float spawnTimer;

        private BuildingInventoryPort startPort;

        // --------------------------------------------------
        // INITIALIZATION
        // --------------------------------------------------

        public void Initialize(BuildingInventory start, BuildingInventoryPort newStartPort, BuildingInventory end)
        {
            startInventory = start;
            endInventory = end;

            startPort = newStartPort;
            startPortName = startPort?.portName;

            startInventory?.RegisterConnection(isInput: false);
            endInventory?.RegisterConnection(isInput: true);
        }

        private void OnDestroy()
        {
            startInventory?.UnregisterConnection(isInput: false);
            endInventory?.UnregisterConnection(isInput: true);
        }

        // --------------------------------------------------
        // MAIN LOOP
        // --------------------------------------------------

        private void Update()
        {
            if (pathTiles == null || pathTiles.Count < 2)
                return;

            if (startPort == null)
                return;

            HandleSpawning();

            // 🚨 If blocked, skip movement entirely
            if (isBlocked)
                return;

            MoveItems();
        }

        // --------------------------------------------------
        // ITEM SPAWNING
        // --------------------------------------------------

        private void HandleSpawning()
        {
            if (isBlocked) return;

            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f) return;
            spawnTimer = spawnDelay;

            if (startPort.IsEmpty)
                return;

            ItemDefinition itemDef = startPort.GetCurrentItemDefinition();
            if (itemDef == null || itemDef.beltPrefab == null)
                return;

            float taken = startPort.Remove(itemDef, 1f);
            if (taken <= 0f)
                return;

            Vector3 startPos = pathTiles[0].transform.position;
            GameObject visual = Instantiate(itemDef.beltPrefab, startPos, Quaternion.identity, transform);
            activeItems.Add(new MovingItem(itemDef, visual));
        }

        // --------------------------------------------------
        // ITEM MOVEMENT
        // --------------------------------------------------

        private void MoveItems()
        {
            for (int i = activeItems.Count - 1; i >= 0; i--)
            {
                var item = activeItems[i];
                if (item == null || item.visual == null)
                    continue;

                item.progress += speed * Time.deltaTime;

                int segment = Mathf.FloorToInt(item.progress);
                float t = item.progress - segment;

                // ✅ If reached the end of the path
                if (segment >= pathTiles.Count - 1)
                {
                    bool delivered = TryDeliverItem(item, i);

                    // 🚨 If cannot deliver → block entire belt
                    if (!delivered)
                    {
                        isBlocked = true;
                        return; // stop moving all items
                    }

                    continue;
                }

                Vector3 a = pathTiles[segment].transform.position;
                Vector3 b = pathTiles[segment + 1].transform.position;
                Vector3 newPos = Vector3.Lerp(a, b, t);

                item.visual.transform.position = newPos;
                item.visual.transform.rotation = GetSegmentRotation(a, b);
            }
        }

        // --------------------------------------------------
        // DELIVERY LOGIC
        // --------------------------------------------------

        private bool TryDeliverItem(MovingItem item, int index)
        {
            if (item == null) return false;

            bool delivered = endInventory.TryInsertItem(item.itemDef, 1f);

            if (delivered)
            {
                Destroy(item.visual);
                activeItems.RemoveAt(index);
                isBlocked = false;
                return true;
            }

            // ❌ Destination full → return to start, mark belt as blocked
            startPort.Add(item.itemDef, 1f);
            return false;
        }

        // --------------------------------------------------
        // UTILITY
        // --------------------------------------------------

        private static Quaternion GetSegmentRotation(Vector3 from, Vector3 to)
        {
            Vector2 dir = (to - from).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            return Quaternion.Euler(0, 0, angle - 90f);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (pathTiles == null || pathTiles.Count == 0)
                return;

            Gizmos.color = isBlocked ? Color.red : Color.green;

            for (int i = 0; i < pathTiles.Count - 1; i++)
                Gizmos.DrawLine(pathTiles[i].transform.position, pathTiles[i + 1].transform.position);

            if (!string.IsNullOrEmpty(startPortName))
                UnityEditor.Handles.Label(pathTiles[0].transform.position + Vector3.up * 0.3f, $"OUT: {startPortName}");
        }
#endif

        public void AddToPath(List<GameObject> newTiles)
        {
            pathTiles.AddRange(newTiles);
        }
    }

    // --------------------------------------------------
    // MOVING ITEM CLASS
    // --------------------------------------------------

    [System.Serializable]
    public class MovingItem
    {
        public ItemDefinition itemDef;
        public GameObject visual;
        public float progress;

        public MovingItem(ItemDefinition def, GameObject visual)
        {
            itemDef = def;
            this.visual = visual;
            progress = 0f;
        }
    }
}
