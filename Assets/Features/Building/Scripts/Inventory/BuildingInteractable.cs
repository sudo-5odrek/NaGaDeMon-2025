using NaGaDeMon.Features.Inventory;
using UnityEngine;
using NaGaDeMon.Features.Player;

namespace NaGaDeMon.Features.Building.Inventory
{
    [RequireComponent(typeof(BuildingInventory))]
    public class BuildingInteractable : MonoBehaviour, IInteractable
    {
        private BuildingInventory buildingInventory;

        private void Awake()
        {
            buildingInventory = GetComponent<BuildingInventory>();
        }

        public void OnHoverEnter() => Debug.Log($"🟢 Hovering {name}");
        public void OnHoverExit()  => Debug.Log($"⚫ Stopped hovering {name}");

        // ------------------------------------------------------------
        // LEFT CLICK — DUMP SELECTED ITEM USING SMART TRIAGE
        // ------------------------------------------------------------
        public void OnInteractHoldLeft(PlayerInventory playerInventory, ItemDefinition selectedItem)
        {
            if (playerInventory == null || selectedItem == null)
                return;

            float amountAvailable = playerInventory.GetAmount(selectedItem);
            if (amountAvailable <= 0f)
                return;

            const float amount = 1f;

            // Try to insert into the building using smart routing
            bool accepted = buildingInventory.TryInsertItem(selectedItem, amount);

            if (!accepted)
            {
                Debug.Log($"❌ {name} rejected {selectedItem.displayName}");
                return;
            }

            // Remove from player AFTER successful insertion
            playerInventory.RemoveItem(selectedItem, amount);

            Debug.Log($"🔄 Transferred {amount}x {selectedItem.displayName} → {name}");
        }

        // ------------------------------------------------------------
        // RIGHT CLICK — TAKE ITEMS FROM BUILDING (same logic as before)
        // ------------------------------------------------------------
        public void OnInteractHoldRight(PlayerInventory playerInventory)
        {
            if (playerInventory == null)
                return;

            var outputPort = buildingInventory.GetOutput() as BuildingInventoryPort;
            if (outputPort == null)
                return;

            TransferBuildingToPlayer(outputPort, playerInventory);
        }

        // ------------------------------------------------------------
        // TRANSFER LOGIC: BUILDING → PLAYER
        // ------------------------------------------------------------
        private void TransferBuildingToPlayer(BuildingInventoryPort port, PlayerInventory player)
        {
            if (port == null || player == null)
                return;

            if (port.IsEmpty)
                return;

            ItemDefinition item = port.itemDefinition;
            if (item == null)
                return;

            const float amount = 1f;

            if (!port.CanProvide(item, amount))
                return;

            float removed = port.Remove(item, amount);
            player.AddItem(item, removed);

            Debug.Log($"🔄 Transferred {removed}x {item.displayName} → Player");

            if (port.Amount <= 0f)
            {
                Debug.Log($"🧹 Port '{port.portName}' is now empty → clearing item type");
                port.itemDefinition = null;
            }
        }
    }
}
