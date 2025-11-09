using UnityEngine;
using Player;
using Interface;

namespace Building.Storage
{
    public class StorageInventory : MonoBehaviour, IInteractable, IInventory
    {
        [Header("Storage Settings")]
        [Tooltip("Total capacity (in resource units). -1 = infinite")]
        [SerializeField] private float capacity = 100f;
        [Tooltip("Time between transfers (seconds per unit).")]
        [SerializeField] private float transferInterval = 0.15f;

        private float transferTimerLeft;
        private float transferTimerRight;

        [Header("Debug")]
        [SerializeField] private float usedCapacity;

        
        public bool HasAnyOutputResource()
        {
            foreach (var kvp in Inventory.GetAll())
            {
                if (kvp.Value > 0f)
                    return true;
            }
            return false;
        }

        public bool HasFreeSpace()
        {
            if (capacity < 0) return true; // infinite capacity
            return Inventory.UsedCapacity < capacity;
        }

        public bool TryAdd(string resourceId, float amount)
        {
            float added = Inventory.Add(resourceId, amount);
            return added > 0f;
        }

        public bool TryRemove(string resourceId, float amount)
        {
            float removed = Inventory.Remove(resourceId, amount);
            return removed > 0f;
        }
        
        // --- Core ---
        public Inventory Inventory { get; private set; }

        private void Awake()
        {
            Inventory = new Inventory(capacity);
            Inventory.OnInventoryChanged += OnInventoryChanged;
        }

        private void OnDestroy()
        {
            if (Inventory != null)
                Inventory.OnInventoryChanged -= OnInventoryChanged;
        }

        private void OnInventoryChanged()
        {
            usedCapacity = Inventory.UsedCapacity;
        }

        // ------------------------------------------------------------
        //  INTERACTION IMPLEMENTATION
        // ------------------------------------------------------------

        public void OnHoverEnter()
        {
            //TooltipSystem.Show($"Chest\n[LMB] Deposit\n[RMB] Withdraw");
        }

        public void OnHoverExit()
        {
            //TooltipSystem.Hide();
        }

        public void OnInteractHoldLeft(PlayerInventory playerInventory)
        {
            transferTimerLeft += Time.deltaTime;
            if (transferTimerLeft < transferInterval)
                return;

            transferTimerLeft -= transferInterval;

            // 🔹 Transfer *one unit of any resource* player has
            foreach (var kvp in playerInventory.Inventory.GetAll())
            {
                string resourceId = kvp.Key;
                if (kvp.Value <= 0) continue;

                float removed = playerInventory.Inventory.Remove(resourceId, 1f);
                float added = Inventory.Add(resourceId, removed);

                if (added > 0f)
                {
                    Debug.Log($"+1 {resourceId} added to chest.");
                    //TooltipSystem.UpdateText($"{resourceId}: {Inventory.Get(resourceId)}");
                    return; // transfer only one unit per tick
                }
            }
        }

        public void OnInteractHoldRight(PlayerInventory playerInventory)
        {
            transferTimerRight += Time.deltaTime;
            if (transferTimerRight < transferInterval)
                return;

            transferTimerRight -= transferInterval;

            // 🔹 Withdraw *one unit of any stored resource*
            foreach (var kvp in Inventory.GetAll())
            {
                string resourceId = kvp.Key;
                if (kvp.Value <= 0) continue;

                float removed = Inventory.Remove(resourceId, 1f);
                float added = playerInventory.Inventory.Add(resourceId, removed);

                if (added > 0f)
                {
                    Debug.Log($"-1 {resourceId} taken from chest.");
                    //TooltipSystem.UpdateText($"{resourceId}: {Inventory.Get(resourceId)}");
                    return;
                }
            }
        }
    }
}
