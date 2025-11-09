using UnityEngine;

namespace Building
{
    public class SharedBuildingInventory : MonoBehaviour, IInventoryAccess
    {
        public float capacity = 20f;
        public string[] acceptedResources;

        private Inventory inventory;

        private void Awake()
        {
            inventory = new Inventory(capacity);
        }
        
        public Inventory RuntimeInventory => inventory; 

        public bool Accepts(string resourceId)
        {
            if (acceptedResources == null || acceptedResources.Length == 0) return true;
            foreach (var r in acceptedResources)
                if (r == resourceId) return true;
            return false;
        }

        public bool CanAccept(string resourceId, float amount = 1f)
            => Accepts(resourceId) && inventory.HasFreeSpace();

        public bool CanProvide(string resourceId, float amount = 1f)
            => Accepts(resourceId) && inventory.Contains(resourceId, amount);

        public float Add(string resourceId, float amount)
            => Accepts(resourceId) ? inventory.Add(resourceId, amount) : 0f;

        public float Remove(string resourceId, float amount)
            => inventory.Remove(resourceId, amount);

        public bool IsFull => !inventory.HasFreeSpace();
        public bool IsEmpty => inventory.IsEmpty();
    }
}