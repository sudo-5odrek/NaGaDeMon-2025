using System;
using UnityEngine;

namespace NaGaDeMon.Features.Building.Inventory
{
    /// <summary>
    /// A flexible building inventory port that handles one specific item type.
    /// Works for both input and output ports.
    /// </summary>
    [Serializable]
    public class BuildingInventoryPort : IInventoryAccess
    {
        public enum PortType { Input, Output, Both }
        public float Amount => inventory.UsedCapacity;
        
        [NonSerialized] public BuildingInventory parentBuilding;
        
        [Header("Port Settings")]
        public string portName = "Port";
        public PortType portType = PortType.Both;

        [Tooltip("The specific item type this port handles (auto-assigned when first used).")]
        public ItemDefinition itemDefinition;

        public float capacity = 20f;
        public float transferRate = 1f;

        [NonSerialized] private global::Inventory.Inventory inventory;
        
        public event Action<ItemDefinition, float> OnItemAdded;
        public event Action<ItemDefinition, float> OnItemRemoved;

        // --------------------------------------------------
        // INITIALIZATION
        // --------------------------------------------------
        public void Init()
        {
            if (inventory == null)
                inventory = new Inventory(capacity);

            // Assign owner name + instance ID
            inventory.OwnerID = $"{parentBuilding.name}.{portName}#{parentBuilding.GetInstanceID()}";
        }

        public global::Inventory.Inventory RuntimeInventory
        {
            get
            {
                if (inventory == null) Init();
                return inventory;
            }
        }

        // --------------------------------------------------
        // ACCEPTANCE LOGIC
        // --------------------------------------------------

        private bool Accepts(ItemDefinition item)
        {
            // ✅ Accept any item if not yet assigned
            if (itemDefinition == null)
                return true;

            // ✅ Otherwise, only accept the same item type
            return itemDefinition == item;
        }

        // --------------------------------------------------
        // CORE METHODS
        // --------------------------------------------------

        public bool CanAccept(ItemDefinition item, float amount = 1f)
        {
            if (item == null || amount <= 0f)
                return false;

            // 🆕 Building-level whitelist check
            if (parentBuilding != null && !parentBuilding.AllowsItem(item))
                return false;

            // Port-level checks
            return (portType == PortType.Input || portType == PortType.Both)
                   && Accepts(item)
                   && inventory.HasFreeSpace();
        }

        public bool CanProvide(ItemDefinition item, float amount = 1f)
            => (portType == PortType.Output || portType == PortType.Both)
               && Accepts(item)
               && !inventory.IsEmpty();

        public float Add(ItemDefinition item, float amount)
        {
            if (item == null || amount <= 0f) return 0f;

            if (!Accepts(item))
                return 0f;

            if (itemDefinition == null)
                itemDefinition = item;

            float added = inventory.Add(item.itemID, amount);

            if (added > 0f)
                OnItemAdded?.Invoke(item, added);

            return added;
        }

        public float Remove(ItemDefinition item, float amount)
        {
            if (item == null || !Accepts(item))
                return 0f;

            float removed = inventory.Remove(item.itemID, amount);

            // Optionally: clear type when empty again
            if (inventory.IsEmpty())
                itemDefinition = null;
            
            OnItemRemoved?.Invoke(item, amount);

            return removed;
        }

        public bool IsFull => !inventory.HasFreeSpace();
        public bool IsEmpty => inventory.IsEmpty();

        // --------------------------------------------------
        // ITEM ACCESSOR
        // --------------------------------------------------

        /// <summary>
        /// Returns the current ItemDefinition assigned to this port.
        /// </summary>
        public ItemDefinition GetCurrentItemDefinition() => itemDefinition;

        /// <summary>
        /// Clears the item type assignment if this port is emptied.
        /// </summary>
        public void ResetItemType()
        {
            if (inventory.IsEmpty())
                itemDefinition = null;
        }
        
        // --------------------------------------------------
        // UTILITY METHODS
        // --------------------------------------------------

        /// <summary>
        /// Returns the current amount of a specific item stored in this port (only valid for input/both ports).
        /// </summary>
        public float GetItemAmount(ItemDefinition item)
        {
            if (inventory == null || item == null)
                return 0f;

            return inventory.Get(item.itemID);
        }

        /// <summary>
        /// Returns how much free capacity remains for this port (only meaningful for output/both ports).
        /// </summary>
        public float GetFreeSpace()
        {
            if (inventory == null)
                return 0f;

            return Mathf.Max(0f, capacity - inventory.GetTotalAmount());
        }
    }
}
