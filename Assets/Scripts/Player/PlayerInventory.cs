using System;
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

        // --- Core data ---
        public Inventory Inventory { get; private set; }

        // --- Events ---
        public event Action OnInventoryChanged;

        private void Awake()
        {
            // Singleton instance
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Inventory = new Inventory(-1); // -1 = infinite capacity
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

        public void AddItem(string resourceId, float amount)
        {
            if (amount <= 0) return;
            Inventory.Add(resourceId, amount);
        }

        public void RemoveItem(string resourceId, float amount)
        {
            if (amount <= 0) return;
            Inventory.Remove(resourceId, amount);
        }

        public float GetAmount(string resourceId)
        {
            return Inventory.Get(resourceId);
        }

        public bool Has(string resourceId, float amount = 1f)
        {
            return Inventory.Contains(resourceId, amount);
        }

        public void Clear()
        {
            Inventory.Clear();
        }

        // ------------------------------------------------------------
        //  WEIGHT / MOVEMENT INTEGRATION
        // ------------------------------------------------------------

        private void HandleInventoryChanged()
        {
            totalWeight = Inventory.GetTotalAmount();
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Returns 1.0 when weight <= buffer, and scales down linearly afterwards.
        /// </summary>
        public float GetMovementSpeedMultiplier()
        {
            float excess = Mathf.Max(0, totalWeight - weightBuffer);
            float slowdown = 1f - (excess * weightSlowdownFactor);
            return Mathf.Clamp(slowdown, 0.3f, 1f); // never go below 30% speed
        }

        public float GetTotalWeight() => totalWeight;
        
#if UNITY_EDITOR
        [SerializeField, TextArea, Tooltip("Runtime debug view of inventory contents.")]
        private string debugInventoryView;

        private void Update()
        {
            // Only update debug string in the editor for convenience
#if UNITY_EDITOR
            if (Inventory != null)
            {
                var items = Inventory.GetAll();
                debugInventoryView = items.Count == 0
                    ? "(empty)"
                    : string.Join("\n", items.Select(kv => $"{kv.Key}: {kv.Value}"));
            }
#endif
        }
#endif

    }
}



