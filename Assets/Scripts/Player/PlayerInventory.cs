using System;
using System.Collections.Generic;
using Inventory;
using UnityEngine;

namespace Player
{
#if UNITY_EDITOR
    using System.Linq;
#endif

    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        [Header("Inventory Settings")]
        [Tooltip("Multiplier applied to movement speed per unit of weight.")]
        [SerializeField] private float weightSlowdownFactor = 0.005f;

        [Tooltip("Base inventory weight before slowdown is noticeable.")]
        [SerializeField] private float weightBuffer = 10f;

        [Header("Debug Info (Read-Only)")]
        [SerializeField] private float totalWeight;

        // --- Core Data ---
        public Inventory.Inventory Inventory { get; private set; }

        // Reference table for item definitions currently in inventory
        private readonly Dictionary<string, ItemDefinition> itemRefs = new();

        // --- Events ---
        public event Action OnInventoryChanged;

        private void Awake()
        {

            Instance = this;
            Inventory = new Inventory.Inventory(-1); // -1 = infinite capacity
            Inventory.OnInventoryChanged += HandleInventoryChanged;
        }

        private void OnDestroy()
        {
            if (Inventory != null)
                Inventory.OnInventoryChanged -= HandleInventoryChanged;
        }

        // ------------------------------------------------------------
        //  INVENTORY MANAGEMENT
        // ------------------------------------------------------------

        /// <summary>
        /// Add an item using a direct ItemDefinition reference.
        /// </summary>
        public void AddItem(ItemDefinition item, float amount)
        {
            if (item == null || amount <= 0f) return;
            
            if (!itemRefs.ContainsKey(item.itemID))
                itemRefs[item.itemID] = item;
            
            Inventory.Add(item.itemID, amount);
        }

        /// <summary>
        /// Remove an item using its ItemDefinition reference.
        /// </summary>
        public void RemoveItem(ItemDefinition item, float amount)
        {
            if (item == null || amount <= 0f) return;

            Inventory.Remove(item.itemID, amount);
            CleanupEmpty(item);
        }

        public float GetAmount(ItemDefinition item)
        {
            return item != null ? Inventory.Get(item.itemID) : 0f;
        }

        public bool Has(ItemDefinition item, float amount = 1f)
        {
            return item != null && Inventory.Contains(item.itemID, amount);
        }

        public void Clear()
        {
            Inventory.Clear();
            itemRefs.Clear();
            totalWeight = 0f;
            OnInventoryChanged?.Invoke();
        }

        private void CleanupEmpty(ItemDefinition item)
        {
            if (item == null) return;
            if (Inventory.Get(item.itemID) <= 0f && itemRefs.ContainsKey(item.itemID))
                itemRefs.Remove(item.itemID);
        }

        // ------------------------------------------------------------
        //  ITEM DEFINITION ACCESS
        // ------------------------------------------------------------

        /// <summary>
        /// Returns all stored items and their quantities.
        /// </summary>
        public Dictionary<ItemDefinition, float> GetAllItems()
        {
            var result = new Dictionary<ItemDefinition, float>();

            foreach (var kvp in Inventory.GetAll())
            {
                string id = kvp.Key;
                float qty = kvp.Value;

                if (itemRefs.TryGetValue(id, out var def))
                    result[def] = qty;
            }

            return result;
        }

        /// <summary>
        /// Returns all unique ItemDefinitions currently held.
        /// </summary>
        public List<ItemDefinition> GetAllDefinitions()
        {
            return new List<ItemDefinition>(itemRefs.Values);
        }

        // ------------------------------------------------------------
        //  WEIGHT / MOVEMENT INTEGRATION
        // ------------------------------------------------------------

        private void HandleInventoryChanged()
        {
            totalWeight = 0f;

            foreach (var kvp in Inventory.GetAll())
            {
                string id = kvp.Key;
                float qty = kvp.Value;

                if (itemRefs.TryGetValue(id, out var def))
                    totalWeight += qty * def.weight;
                else
                    totalWeight += qty; // default weight = 1 if no def
            }

            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Returns 1.0 when weight <= buffer, and scales down linearly afterwards.
        /// </summary>
        public float GetMovementSpeedMultiplier()
        {
            float excess = Mathf.Max(0, totalWeight - weightBuffer);
            float slowdown = 1f - (excess * weightSlowdownFactor);
            return Mathf.Clamp(slowdown, 0.3f, 1f);
        }

        public float GetTotalWeight() => totalWeight;

        // ------------------------------------------------------------
        //  DEBUG VIEW (Editor only)
        // ------------------------------------------------------------

#if UNITY_EDITOR
        [SerializeField, TextArea, Tooltip("Runtime debug view of inventory contents.")]
        private string debugInventoryView;

        private void Update()
        {
            if (Inventory == null) return;

            var items = GetAllItems();
            debugInventoryView = items.Count == 0
                ? "(empty)"
                : string.Join("\n", items.Select(kv => $"{kv.Key.displayName}: {kv.Value}"));
        }
#endif
    }
}
