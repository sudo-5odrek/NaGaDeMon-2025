using Interface;
using UnityEngine;
using Player;

namespace Building
{
    [RequireComponent(typeof(BuildingInventory))]
    public class BuildingInteractable : MonoBehaviour, IInteractable
    {
        private BuildingInventory buildingInventory;

        private void Awake()
        {
            buildingInventory = GetComponent<BuildingInventory>();
        }

        public void OnHoverEnter() => Debug.Log($"Hovering {name}");
        public void OnHoverExit() => Debug.Log($"Stopped hovering {name}");

        // Player left-click = dump into building
        public void OnInteractHoldLeft(PlayerInventory playerInventory)
        {
            TransferItem(playerInventory.Inventory, GetBuildingInventory());
        }

        // Player right-click = take from building
        public void OnInteractHoldRight(PlayerInventory playerInventory)
        {
            TransferItem(GetBuildingInventory(), playerInventory.Inventory);
        }

        private Inventory GetBuildingInventory()
        {
            if (buildingInventory.useSingleSharedInventory)
                return buildingInventory.SharedRuntimeInventory;
            else
                return buildingInventory.ports.Count > 0
                    ? buildingInventory.ports[0].inventory
                    : null;
        }

        private void TransferItem(Inventory from, Inventory to)
        {
            if (from == null || to == null) return;

            // very basic example: try to move the first resource type
            var all = from.GetAll();
            foreach (var kvp in all)
            {
                string resourceId = kvp.Key;
                float amount = Mathf.Min(kvp.Value, 1f); // 1 unit per frame or tick
                if (from.Contains(resourceId, amount) && to.HasFreeSpace())
                {
                    float removed = from.Remove(resourceId, amount);
                    to.Add(resourceId, removed);
                }
                break;
            }
        }
    }
}
