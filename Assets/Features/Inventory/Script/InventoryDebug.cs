using NaGaDeMon.Features.Player;
using UnityEngine;

namespace NaGaDeMon.Features.Inventory
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
