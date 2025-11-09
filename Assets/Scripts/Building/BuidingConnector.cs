using UnityEngine;
using System.Collections.Generic;
using Grid;

namespace Building
{
    /// <summary>
    /// Handles logical links between buildings (inputs/outputs)
    /// and mediates resource flow with the building's inventory.
    /// Works with both SharedInventory and Port-based setups.
    /// </summary>
    public class BuildingConnector : MonoBehaviour
    {
        [Header("Connection Settings")]
        [Tooltip("Whether this building can output resources/connections.")]
        public bool canOutput = true;

        [Tooltip("Whether this building can receive input from another source.")]
        public bool canInput = true;

        [Tooltip("Maximum number of outgoing connections allowed.")]
        public int maxOutputs = 1;

        [Tooltip("Maximum number of input connections allowed.")]
        public int maxInputs = 1;

        [Header("Inventory Link")]
        [Tooltip("Reference to this building's inventory component (shared or multi-port).")]
        public BuildingInventory buildingInventory;

        [Tooltip("If using multi-port inventory, specify which port this connector uses.")]
        public string portName;

        public Vector3Int GridOrigin { get; private set; }

        public int currentInputAmount;
        public int currentOutputAmount;

        // --- Private cache ---
        private IInventoryAccess inventoryPort;

        // --------------------------------------------------
        // INITIALIZATION
        // --------------------------------------------------

        private void Start()
        {
            (int gx, int gy) = GridManager.Instance.GridFromWorld(transform.position);
            GridOrigin = new Vector3Int(gx, gy, 0);

            CacheInventoryAccess();
        }

        /// <summary>
        /// Resolves and caches the correct inventory access (shared or port-based).
        /// </summary>
        private void CacheInventoryAccess()
        {
            if (buildingInventory == null)
            {
                Debug.LogWarning($"[{name}] No BuildingInventory linked to connector!");
                return;
            }

            // ✅ SHARED INVENTORY MODE
            if (buildingInventory.useSingleSharedInventory)
            {
                if (buildingInventory.SharedRuntimeInventory != null)
                {
                    inventoryPort = new InventoryProxy(buildingInventory.SharedRuntimeInventory);
                    Debug.Log($"[{name}] Using shared inventory from {buildingInventory.name}");
                }
                else
                {
                    Debug.LogWarning($"[{name}] Shared inventory enabled but not assigned!");
                }
                return;
            }

            // ✅ MULTI-PORT MODE
            if (!string.IsNullOrEmpty(portName))
            {
                var port = buildingInventory.ports.Find(p => p.portName == portName);
                if (port != null)
                    inventoryPort = port;
                else
                    Debug.LogWarning($"[{name}] No port named '{portName}' found on {buildingInventory.name}.");
            }
            else
            {
                // fallback — grab first matching direction
                if (canOutput)
                    inventoryPort = buildingInventory.GetOutput();
                else
                    inventoryPort = buildingInventory.GetInput();
            }
        }

        // --------------------------------------------------
        // STATE CHECKS
        // --------------------------------------------------

        public bool CanAcceptInput() =>
            canInput;

        public bool CanProvideOutput() =>
            canOutput;

        public bool IsFull() => inventoryPort != null && inventoryPort.IsFull;
        public bool IsEmpty() => inventoryPort == null || inventoryPort.IsEmpty;

        // --------------------------------------------------
        // INVENTORY ACCESS
        // --------------------------------------------------

        public float TryInsertResource(string resourceId, float amount = 1f)
        {
            if (!canInput || inventoryPort == null) return 0f;
            return inventoryPort.Add(resourceId, amount);
        }

        public float TryExtractResource(string resourceId, float amount = 1f)
        {
            if (!canOutput || inventoryPort == null) return 0f;
            return inventoryPort.Remove(resourceId, amount);
        }

        public bool HasResource(string resourceId, float amount = 1f)
        {
            if (inventoryPort == null) return false;
            return inventoryPort.CanProvide(resourceId, amount);
        }

        public bool CanReceive(string resourceId, float amount = 1f)
        {
            if (inventoryPort == null) return false;
            return inventoryPort.CanAccept(resourceId, amount);
        }

        // --------------------------------------------------
        // CONNECTION REGISTRATION
        // --------------------------------------------------

        public void RegisterOutputConnection(Vector3 worldPosition)
        {
            if (!CanProvideOutput())
            {
                Debug.LogWarning($"[{name}] Reached max output limit ({maxOutputs}).");
                return;
            }
            currentOutputAmount++;
            if (currentOutputAmount >= maxOutputs)
                canOutput = false;
        }

        public void RegisterInput(BuildingConnector source)
        {
            if (!CanAcceptInput())
            {
                Debug.LogWarning($"[{name}] Reached max input limit ({maxInputs}).");
                return;
            }

            currentInputAmount++;
            if (currentInputAmount >= maxInputs)
                canInput = false;
        }

        public void ClearConnections()
        {
            currentInputAmount = 0;
            currentOutputAmount = 0;
        }
    }

    /// <summary>
    /// Helper adapter to make a plain Inventory behave as IInventoryAccess.
    /// Used for shared inventories so we can treat them like ports.
    /// </summary>
    internal class InventoryProxy : IInventoryAccess
    {
        private readonly Inventory inventory;
        public InventoryProxy(Inventory inventory) => this.inventory = inventory;

        public bool CanAccept(string resourceId, float amount = 1f) => inventory.HasFreeSpace();
        public bool CanProvide(string resourceId, float amount = 1f) => inventory.Contains(resourceId, amount);
        public float Add(string resourceId, float amount) => inventory.Add(resourceId, amount);
        public float Remove(string resourceId, float amount) => inventory.Remove(resourceId, amount);
        public bool IsFull => !inventory.HasFreeSpace();
        public bool IsEmpty => inventory.IsEmpty();
    }
}
