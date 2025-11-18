using System;
using System.Collections.Generic;
using UnityEngine;

namespace Inventory
{
    /// <summary>
    /// Core runtime inventory class — no MonoBehaviour.
    /// Used by both PlayerInventory and BuildingInventory.
    /// </summary>
    [System.Serializable]
    public class Inventory
    {
        // --- EVENTS ---
        public event Action OnInventoryChanged;
        
        public string OwnerID = "UnknownOwner";

        // --- DATA ---
        [SerializeField] private float totalCapacity = -1f; // -1 = infinite
        [SerializeField] private Dictionary<string, float> items = new();
    
        public bool HasAnyOutputResource() => items.Count > 0;
        public bool HasFreeSpace() => GetTotalAmount() < totalCapacity;

        // --- PROPERTIES ---
        public float TotalCapacity => totalCapacity;
        public bool HasCapacityLimit => totalCapacity > 0;
        public float UsedCapacity => GetTotalAmount();
        public float FreeCapacity => HasCapacityLimit ? Mathf.Max(0, totalCapacity - UsedCapacity) : float.PositiveInfinity;

        // --- CONSTRUCTORS ---
        public Inventory(float totalCapacity = -1f)
        {
            this.totalCapacity = totalCapacity;
        }

        // ------------------------------------------------------------
        //  CORE METHODS
        // ------------------------------------------------------------

        /// <summary>
        /// Add an amount of resource to the inventory. Returns the actual amount added.
        /// </summary>
        public float Add(string resourceId, float amount)
        {
            if (amount <= 0f) return 0f;

            float space = HasCapacityLimit ? Mathf.Min(FreeCapacity, amount) : amount;
            if (space <= 0f) return 0f;

            if (!items.ContainsKey(resourceId))
                items[resourceId] = 0f;

            items[resourceId] += space;

            // 🔍 DEBUG LOG
            Debug.Log($"[INVENTORY ADD] ({OwnerID}) Added {space} of '{resourceId}'");
            foreach (var kvp in items)
                Debug.Log($"    ({OwnerID}) - {kvp.Key} = {kvp.Value}");

            OnInventoryChanged?.Invoke();
            return space;
        }

        /// <summary>
        /// Remove an amount of resource. Returns the actual amount removed.
        /// </summary>
        public float Remove(string resourceId, float amount)
        {
            if (amount <= 0f) return 0f;
            if (!items.ContainsKey(resourceId)) return 0f;

            float available = items[resourceId];
            float removed = Mathf.Min(available, amount);
            items[resourceId] -= removed;

            if (items[resourceId] <= 0.0001f)
                items[resourceId] = 0f;

            OnInventoryChanged?.Invoke();
            return removed;
        }

        /// <summary>
        /// Returns the amount of the given resource.
        /// </summary>
        public float Get(string resourceId)
        {
            float value = items.TryGetValue(resourceId, out var v) ? v : 0f;

            Debug.Log($"[INVENTORY GET] ({OwnerID}) Request: '{resourceId}' → Returned: {value}");

            foreach (var kvp in items)
                Debug.Log($"    ({OwnerID}) - Stored {kvp.Key} = {kvp.Value}");

            return value;
        }

        /// <summary>
        /// Checks if the inventory contains at least this amount of a resource.
        /// </summary>
        public bool Contains(string resourceId, float amount = 1f)
        {
            return Get(resourceId) >= amount;
        }

        /// <summary>
        /// Returns total sum of all resource quantities.
        /// </summary>
        public float GetTotalAmount()
        {
            float total = 0f;
            foreach (var kvp in items)
                total += kvp.Value;
            return total;
        }

        /// <summary>
        /// Returns true if no items are stored.
        /// </summary>
        public bool IsEmpty() => items.Count == 0;

        /// <summary>
        /// Remove everything from inventory.
        /// </summary>
        public void Clear()
        {
            items.Clear();
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Returns a copy of the stored items.
        /// </summary>
        public Dictionary<string, float> GetAll()
        {
            return new Dictionary<string, float>(items);
        }

        /// <summary>
        /// Transfers a resource between two inventories safely.
        /// </summary>
        public static bool TransferTickBased(
            Inventory from,
            Inventory to,
            string resourceId,
            float transferInterval,
            ref float timer)
        {
            if (from == null || to == null || string.IsNullOrEmpty(resourceId))
                return false;

            timer += Time.deltaTime;
            if (timer < transferInterval)
                return false; // ⏳ not yet time for the next tick

            timer -= transferInterval; // reset timer for next cycle

            // Try to move 1 unit
            if (!from.Contains(resourceId, 1f))
                return false;

            float removed = from.Remove(resourceId, 1f);
            float added = to.Add(resourceId, removed);

            // Return excess if target full
            if (added < removed)
                from.Add(resourceId, removed - added);

            return added > 0f;
        }
    }
}
