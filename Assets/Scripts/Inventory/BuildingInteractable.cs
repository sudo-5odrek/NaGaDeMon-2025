using Interface;
using Inventory;
using UnityEngine;
using Player;

namespace Building
{
    /// <summary>
    /// Allows the player to interact with a building’s inventory system.
    /// Left-click: dump player items into building.
    /// Right-click: take items out of the building.
    /// </summary>
    [RequireComponent(typeof(BuildingInventory))]
    public class BuildingInteractable : MonoBehaviour, IInteractable
    {
        private BuildingInventory buildingInventory;

        private void Awake()
        {
            buildingInventory = GetComponent<BuildingInventory>();
        }

        public void OnHoverEnter() => Debug.Log($"🟢 Hovering {name}");
        public void OnHoverExit() => Debug.Log($"⚫ Stopped hovering {name}");

        // ------------------------------------------------------------
        //  PLAYER INTERACTION
        // ------------------------------------------------------------

        /// <summary>
        /// Player left-clicks: dump first carried item into the building.
        /// </summary>
        public void OnInteractHoldLeft(PlayerInventory playerInventory)
        {
            var port = buildingInventory.GetInput() as BuildingInventoryPort;
            if (port == null) return;

            TransferFromPlayerToBuilding(playerInventory, port);
        }

        /// <summary>
        /// Player right-clicks: take resources from the building.
        /// </summary>
        public void OnInteractHoldRight(PlayerInventory playerInventory)
        {
            var port = buildingInventory.GetOutput() as BuildingInventoryPort;
            if (port == null) return;

            TransferFromBuildingToPlayer(port, playerInventory);
        }

        // ------------------------------------------------------------
        //  TRANSFER LOGIC
        // ------------------------------------------------------------

        private void TransferFromPlayerToBuilding(PlayerInventory player, BuildingInventoryPort buildingPort)
        {
            if (player == null || buildingPort == null) return;
            if (player.Inventory.IsEmpty()) return;

            // get first item in player inventory
            var all = player.Inventory.GetAll();
            var first = all.Count > 0 ? all.GetEnumerator() : default;
            if (all.Count == 0) return;
            first.MoveNext();
            var kvp = first.Current;
            string resourceId = kvp.Key;
            float amount = Mathf.Min(kvp.Value, 1f);

            // find corresponding ItemDefinition from player's known items
            ItemDefinition itemDef = null;
            var allDefs = player.GetAllItems();
            foreach (var defPair in allDefs)
            {
                if (defPair.Key.itemID == resourceId)
                {
                    itemDef = defPair.Key;
                    break;
                }
            }

            if (itemDef == null)
            {
                Debug.LogWarning($"[BuildingInteractable] Could not find ItemDefinition for {resourceId}");
                return;
            }

            // auto-assign the port's definition if it's empty
            if (buildingPort.itemDefinition == null)
            {
                buildingPort.itemDefinition = itemDef;
                Debug.Log($"🏗️ Port '{buildingPort.portName}' learned new item type: {itemDef.displayName}");
            }

            // if item type doesn't match, skip
            if (buildingPort.itemDefinition != itemDef)
                return;

            // transfer 1 unit
            if (!player.Inventory.Contains(resourceId, amount) || !buildingPort.CanAccept(itemDef, amount))
                return;

            float removed = player.Inventory.Remove(resourceId, amount);
            float added = buildingPort.Add(itemDef, removed);

            Debug.Log($"🔄 Transferred {added}x {itemDef.displayName} → {buildingPort.portName}");
        }

        private void TransferFromBuildingToPlayer(BuildingInventoryPort buildingPort, PlayerInventory player)
        {
            if (buildingPort == null || player == null) return;
            if (buildingPort.IsEmpty) return;

            var itemDef = buildingPort.itemDefinition;
            if (itemDef == null)
                return;

            float amount = 1f;
            if (!buildingPort.CanProvide(itemDef, amount))
                return;

            float removed = buildingPort.Remove(itemDef, amount);
            player.AddItem(itemDef, removed);

            Debug.Log($"🔄 Transferred {removed}x {itemDef.displayName} → Player");
        }
    }
}
