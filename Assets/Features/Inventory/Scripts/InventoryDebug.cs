using Features.Player.Scripts;
using UnityEngine;

namespace Features.Inventory.Scripts
{
    public class InventoryDebug : MonoBehaviour
    {
        public ItemDatabase database;

        public void AddAllToInventory()
        {
            foreach (ItemDefinition item in database.allItems)
            {
                PlayerInventory.Instance.AddItem(item, 999);
            }
        }
    
        public void RemoveAllFromInventory()
        {
            foreach (ItemDefinition item in database.allItems)
            {
                PlayerInventory.Instance.RemoveItem(item, PlayerInventory.Instance.GetAmount(item));
            }
        }
    }
}
