using UnityEngine;
using Inventory;
using System.Collections.Generic;

namespace Building.Production
{
    public class MinerBuilding : MonoBehaviour, IHasProgress
    {
        [Header("Mining Node")]
        public MiningNode miningNode; // The resource node the miner is attached to

        [Header("Mining Settings")]
        public float mineInterval = 1.5f;       // seconds between mining cycles
        public float mineAmount = 1f;           // amount generated each cycle

        [Header("References")]
        public BuildingInventory buildingInventory;

        private float timer;
        private List<BuildingInventoryPort> outputPorts = new();
        
        public bool IsProcessing => timer < mineInterval;
        
        
        
        public void AssignNode(MiningNode node)
        {
            miningNode = node;
        }
        
        public float ProgressNormalized
        {
            get
            {
                float t = 1f - (timer / mineInterval);
                return Mathf.Clamp01(t);
            }
        }

        private void Start()
        {
            if (buildingInventory == null)
            {
                Debug.LogError($"{name}: Missing BuildingInventory!");
                enabled = false;
                return;
            }

            if (miningNode == null)
            {
                Debug.LogError($"{name}: No MiningNode assigned!");
                enabled = false;
                return;
            }

            CacheOutputPorts();
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0) return;

            timer = mineInterval;
            MineTick();
        }

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

        private void MineTick()
        {
            float remaining = mineAmount;
            ItemDefinition item = miningNode.resourceItem;

            foreach (var port in outputPorts)
            {
                if (remaining <= 0) break;

                remaining -= port.Add(item, remaining);
            }

            // If output ports are full, remaining > 0
            // You can handle overflow (e.g., pause mining) if you want
        }
    }
}
