using System.Collections.Generic;
using Features.Player.Scripts;
using UnityEngine;

namespace Features.Inventory.Scripts
{
    public class InventoryUIBootstrapper : MonoBehaviour
    {
        [Header("Data")]
        public ItemDatabase itemDatabase;

        [Header("UI")]
        public Transform entriesParent;
        public InventoryUIItemEntry entryPrefab;

        [Header("References")]
        private PlayerInventory _playerInventory;

        private readonly Dictionary<ItemDefinition, InventoryUIItemEntry> entries =
            new Dictionary<ItemDefinition, InventoryUIItemEntry>();


        private void Awake()
        {
            if (_playerInventory == null)
                _playerInventory = PlayerInventory.Instance;

            BuildStaticUI();
        }


        private void Start()
        {
            // Subscribe here — AFTER PlayerInventory.Awake has run
            if (_playerInventory != null)
            {
                _playerInventory.OnInventoryChanged += RefreshAllEntries;
                RefreshAllEntries();
            }
        }

        private void OnDestroy()
        {
            if (_playerInventory != null)
                _playerInventory.OnInventoryChanged -= RefreshAllEntries;
        }


        // ------------------------------------------------------------
        //  BUILD UI FROM ITEM DATABASE
        // ------------------------------------------------------------
        private void BuildStaticUI()
        {
            entries.Clear();

            foreach (var item in itemDatabase.allItems)
            {
                if (item == null) continue;

                var entry = Instantiate(entryPrefab, entriesParent);
                entry.Setup(item, 0);

                entries[item] = entry;
            }

            // initialize with current player inventory
            if (_playerInventory != null)
                RefreshAllEntries();
        }


        // ------------------------------------------------------------
        //  UPDATE UI WHEN INVENTORY CHANGES
        // ------------------------------------------------------------
        private void RefreshAllEntries()
        {
            if (_playerInventory == null) return;

            // 1) Reset all entries to 0 first (so removed items show as 0)
            foreach (var entry in entries.Values)
            {
                entry.SetCount(0);
            }

            // 2) Get all items currently stored in the player inventory
            Dictionary<ItemDefinition, float> playerItems = _playerInventory.GetAllItems();

            // 3) For each item in the player inventory, update the matching UI entry (if any)
            foreach (var kvp in playerItems)
            {
                ItemDefinition itemDef = kvp.Key;
                float amount = kvp.Value;

                if (entries.TryGetValue(itemDef, out InventoryUIItemEntry entry))
                {
                    entry.SetCount((int)amount);
                }
                // else: this item exists in inventory but not in the UI database → skip
            }
        }
    }
}
