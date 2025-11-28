using UnityEngine;
using Inventory;
using System.Collections.Generic;

namespace Building.Production
{
    public class MinerBuilding : MonoBehaviour, IHasProgress
    {
        [Header("Mining Node")]
        public MiningNode miningNode; // Automatically detected or assigned

        [Header("Mining Settings")]
        public float mineInterval = 1.5f;
        public float mineAmount = 1f;

        [Header("References")]
        public BuildingInventory buildingInventory;

        private float timer;
        private List<BuildingInventoryPort> outputPorts = new();

        public bool IsProcessing => timer < mineInterval;

        // =====================================================================
        // INITIALIZATION
        // =====================================================================
        private void Start()
        {
            // 1. Validate inventory
            if (buildingInventory == null)
            {
                Debug.LogError($"{name}: Missing BuildingInventory!");
                enabled = false;
                return;
            }

            // 2. Ensure node is detected or already assigned
            if (miningNode == null)
            {
                miningNode = DetectNodeUnderBuilding();
                if (miningNode == null)
                {
                    Debug.LogError($"{name}: No MiningNode found underneath!");
                    enabled = false;
                    return;
                }
            }

            // 3. Disable node collider (cannot manually collect anymore)
            DisableNodeCollider(miningNode);

            // 4. Cache output ports
            CacheOutputPorts();
        }

        // =====================================================================
        // NODE DETECTION
        // =====================================================================
        private MiningNode DetectNodeUnderBuilding()
        {
            // Use a very small overlap circle or point cast at building center
            Vector3 pos = transform.position;

            Collider2D col = Physics2D.OverlapPoint(pos);
            if (col != null && col.TryGetComponent(out MiningNode node))
                return node;

            // If the collider doesn't match exactly, try a small radius
            Collider2D[] hits = Physics2D.OverlapCircleAll(pos, 0.2f);
            foreach (var h in hits)
            {
                if (h.TryGetComponent(out MiningNode n))
                    return n;
            }

            return null;
        }

        private void DisableNodeCollider(MiningNode node)
        {
            Collider2D col = node.GetComponent<Collider2D>();
            if (col)
            {
                col.enabled = false;
                // Debug.Log($"⛏️ Disabled mining node collider on: {node.name}");
            }
        }

        // =====================================================================
        // MINING LOOP
        // =====================================================================
        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0) return;

            timer = mineInterval;
            MineTick();
        }

        private void MineTick()
        {
            if (miningNode == null) return;

            float remaining = mineAmount;
            ItemDefinition item = miningNode.resourceItem;

            foreach (var port in outputPorts)
            {
                if (remaining <= 0) break;
                remaining -= port.Add(item, remaining);
            }
        }

        // =====================================================================
        // PORTS
        // =====================================================================
        private void CacheOutputPorts()
        {
            outputPorts.Clear();

            foreach (var port in buildingInventory.ports)
            {
                if (port.portType != BuildingInventoryPort.PortType.Input)
                    outputPorts.Add(port);
            }

            if (outputPorts.Count == 0)
                Debug.LogWarning($"{name}: Miner has no output ports!");
        }

        // =====================================================================
        // PROGRESS UI
        // =====================================================================
        public float ProgressNormalized
        {
            get
            {
                float t = 1f - (timer / mineInterval);
                return Mathf.Clamp01(t);
            }
        }
    }
}
