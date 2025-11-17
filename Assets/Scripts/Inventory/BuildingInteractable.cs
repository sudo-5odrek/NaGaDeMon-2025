using Interface;
using Inventory;
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

        public void OnHoverEnter() => Debug.Log($"🟢 Hovering {name}");
        public void OnHoverExit()  => Debug.Log($"⚫ Stopped hovering {name}");

        // ------------------------------------------------------------
        // LEFT CLICK — DUMP SELECTED ITEM TYPE
        // ------------------------------------------------------------
        public void OnInteractHoldLeft(PlayerInventory playerInventory, ItemDefinition selectedItem)
        {
            if (playerInventory == null || selectedItem == null)
                return;

            var inputPort = buildingInventory.GetInput() as BuildingInventoryPort;
            if (inputPort == null)
                return;

            TransferPlayerToBuilding(playerInventory, inputPort, selectedItem);
        }

        // ------------------------------------------------------------
        // RIGHT CLICK — TAKE ITEMS FROM BUILDING
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
        // TRANSFER LOGIC: PLAYER → BUILDING
        // ------------------------------------------------------------
        private void TransferPlayerToBuilding(PlayerInventory player, BuildingInventoryPort port, ItemDefinition item)
        {
            if (player == null || port == null || item == null)
                return;

            // Check if player has this item at all.
            float amountAvailable = player.GetAmount(item);
            if (amountAvailable <= 0f)
                return;

            float amount = Mathf.Min(1f, amountAvailable); // 1 unit per frame
            

            // If port only accepts a different type → skip
            if (port.itemDefinition != null && port.itemDefinition != item)
                return;

            // Check acceptance & execute transfer
            if (!port.CanAccept(item, amount))
                return;

            player.RemoveItem(item, amount);
            float removed = amount;
            float added = port.Add(item, removed);

            Debug.Log($"🔄 Transferred {added}x {item.displayName} → {port.portName}");
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

            // 🆕 Reset item type if port is now empty
            if (port.Amount <= 0f)
            {
                Debug.Log($"🧹 Port '{port.portName}' is now empty → clearing item type");
                port.itemDefinition = null;
            }
        }
    }
}
