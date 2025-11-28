using System.Collections.Generic;
using NaGaDeMon.Features.Inventory;
using UnityEngine;

namespace Features.Inventory.Scripts
{
    [CreateAssetMenu(
        fileName = "ItemDatabase",
        menuName = "TD/Inventory/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [Tooltip("All items that can appear in the game and be tracked by the inventory UI.")]
        public List<ItemDefinition> allItems = new List<ItemDefinition>();

        /// <summary>
        /// Optional helper to get an item by some ID string, if ItemDefinition has one.
        /// </summary>
        public ItemDefinition GetById(string id)
        {
            foreach (var item in allItems)
            {
                if (item != null && item.itemID == id) // adapt if your field name differs
                    return item;
            }

            Debug.LogWarning($"[ItemDatabase] No item with id '{id}' found.");
            return null;
        }
    }
}
