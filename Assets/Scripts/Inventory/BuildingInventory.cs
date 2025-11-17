using System.Collections.Generic;
using Building;
using UnityEngine;

namespace Inventory
{
    /// <summary>
    /// Centralized inventory system for a building.
    /// Manages all ports, their connections, capacity limits,
    /// and provides access/control methods for belts and logic systems.
    /// </summary>
    [DisallowMultipleComponent]
    public class BuildingInventory : MonoBehaviour
    {
        [Header("Port Configuration")]
        [Tooltip("List of inventory ports attached to this building.")]
        public List<BuildingInventoryPort> ports = new();

        [Header("Connection Limits")]
        [Tooltip("Maximum number of incoming belts or connections allowed.")]
        public int maxInputs = 2;

        [Tooltip("Maximum number of outgoing belts or connections allowed.")]
        public int maxOutputs = 2;

        private int currentInputs;
        private int currentOutputs;
        
        public BuildingData data;

        // --------------------------------------------------
        // LIFECYCLE
        // --------------------------------------------------

        private void Awake()
        {
            foreach (var port in ports)
            {
                port.parentBuilding = this;
                port.Init();
            }
        }

        // --------------------------------------------------
        // INVENTORY ACCESSORS
        // --------------------------------------------------
        
        public bool AllowsItem(ItemDefinition item)
        {
            // --- Building missing data ---
            if (data == null)
            {
                Debug.LogWarning($"[{name}] No BuildingData assigned → accepting all items by default.");
                return true;
            }

            // --- Accept-all toggle ---
            if (data.acceptAllItems)
            {
                Debug.Log($"[{name}] Accepting {item?.displayName ?? "NULL"} because acceptAllItems = TRUE.");
                return true;
            }

            // --- Null item (should never happen, but safe to check) ---
            if (item == null)
            {
                Debug.LogWarning($"[{name}] Rejecting item: NULL (building requires whitelist items).");
                return false;
            }

            // --- Check whitelist ---
            bool allowed = data.itemWhitelist.Contains(item);

            if (allowed)
                Debug.Log($"[{name}] Accepting {item.displayName} — found in whitelist.");
            else
                Debug.Log($"[{name}] Rejecting {item.displayName} — NOT in whitelist.");

            return allowed;
        }

        /// <summary>
        /// Returns a specific port by name.
        /// </summary>
        public BuildingInventoryPort GetPort(string portName)
            => ports.Find(p => p.portName == portName);

        /// <summary>
        /// Returns the first available input-compatible port.
        /// </summary>
        public IInventoryAccess GetInput()
        {
            return ports.Find(p =>
                p.portType == BuildingInventoryPort.PortType.Input ||
                p.portType == BuildingInventoryPort.PortType.Both);
        }

        /// <summary>
        /// Returns the first available output-compatible port.
        /// </summary>
        public IInventoryAccess GetOutput()
        {
            return ports.Find(p =>
                p.portType == BuildingInventoryPort.PortType.Output ||
                p.portType == BuildingInventoryPort.PortType.Both);
        }

        /// <summary>
        /// Returns the shared inventory port (the first one marked Both).
        /// </summary>
        private IInventoryAccess GetSharedInventory()
        {
            var shared = ports.Find(p => p.portType == BuildingInventoryPort.PortType.Both);
            if (shared == null)
                Debug.LogWarning($"[{name}] Shared inventory requested but none marked as 'Both'.");
            return shared;
        }

        // --------------------------------------------------
        // CONNECTION MANAGEMENT
        // --------------------------------------------------

        public bool CanAcceptNewInput => currentInputs < maxInputs;
        public bool CanAcceptNewOutput => currentOutputs < maxOutputs;

        public void RegisterConnection(bool isInput)
        {
            if (isInput) currentInputs++;
            else currentOutputs++;
        }

        public void UnregisterConnection(bool isInput)
        {
            if (isInput && currentInputs > 0) currentInputs--;
            else if (!isInput && currentOutputs > 0) currentOutputs--;
        }

        // --------------------------------------------------
        // INVENTORY QUERIES (Updated for ItemDefinition)
        // --------------------------------------------------

        public bool IsPortFull(string portName)
        {
            var port = GetPort(portName);
            return port?.IsFull ?? true;
        }

        public bool IsPortEmpty(string portName)
        {
            var port = GetPort(portName);
            return port?.IsEmpty ?? true;
        }

        /// <summary>
        /// Returns true if this port can output its assigned item.
        /// </summary>
        public bool CanPushFrom(string portName)
        {
            var port = GetPort(portName);
            if (port == null) return false;

            var item = port.itemDefinition;
            return item != null && port.CanProvide(item, 1f);
        }

        /// <summary>
        /// Returns true if this port can accept its assigned item (or any if unassigned).
        /// </summary>
        public bool CanPullInto(string portName, ItemDefinition item = null)
        {
            var port = GetPort(portName);
            if (port == null) return false;

            // If no item passed, test using port's own item type (or null if uninitialized)
            var targetItem = item ?? port.itemDefinition;
            return port.CanAccept(targetItem, 1f);
        }

        // --------------------------------------------------
        // DEBUG VISUALIZATION
        // --------------------------------------------------

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;

            Vector3 pos = transform.position + Vector3.up * 1.5f;
            string label = $"Inputs: {currentInputs}/{maxInputs} | Outputs: {currentOutputs}/{maxOutputs}\n";

            foreach (var port in ports)
            {
                var inv = port.RuntimeInventory;
                if (inv == null) continue;

                string itemName = port.itemDefinition ? port.itemDefinition.displayName : "(unassigned)";
                label += $"[{port.portName}] ({port.portType}) {itemName}: ";

                foreach (var kvp in inv.GetAll())
                    label += $"{kvp.Key}:{kvp.Value} ";

                label += "\n";
            }

            UnityEditor.Handles.Label(pos, label);
        }
#endif
    }
}
