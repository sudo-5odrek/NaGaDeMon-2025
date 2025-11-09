using UnityEngine;
using System.Collections.Generic;

namespace Building
{
    public class BuildingInventory : MonoBehaviour
    {
        [Header("Mode")]
        public bool useSingleSharedInventory = false;

        [Header("Shared inventory (if enabled)")]
        public SharedBuildingInventory sharedInventory;

        [Header("Port inventories (if not shared)")]
        public List<InventoryPort> ports = new();

        public Inventory SharedRuntimeInventory => sharedInventory?.RuntimeInventory;
        public List<InventoryPort> Ports => ports;

        
        private void Awake()
        {
            if (!useSingleSharedInventory)
                foreach (var port in ports) port.Init();
        }

        public IInventoryAccess GetInput()
        {
            if (useSingleSharedInventory) return sharedInventory;
            return ports.Find(p => p.isInput);
        }

        public IInventoryAccess GetOutput()
        {
            if (useSingleSharedInventory) return sharedInventory;
            return ports.Find(p => !p.isInput);
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;

            Vector3 pos = transform.position + Vector3.up * 1.5f;
            string label = "";

            if (useSingleSharedInventory && sharedInventory?.RuntimeInventory != null)
            {
                foreach (var kvp in sharedInventory.RuntimeInventory.GetAll())
                    label += $"{kvp.Key}:{kvp.Value}  ";
            }
            else
            {
                foreach (var port in ports)
                {
                    label += $"[{port.portName}] ";
                    foreach (var kvp in port.inventory.GetAll())
                        label += $"{kvp.Key}:{kvp.Value} ";
                    label += "\n";
                }
            }

#if UNITY_EDITOR
            UnityEditor.Handles.Label(pos, label);
#endif
        }
    }
    
    
}