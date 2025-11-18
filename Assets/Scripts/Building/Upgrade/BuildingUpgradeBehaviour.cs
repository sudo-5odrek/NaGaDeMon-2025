using System.Collections.Generic;
using Inventory;
using UnityEngine;

namespace Building.Upgrade
{
    public class BuildingUpgradeBehaviour : MonoBehaviour
    {
        [Header("Upgrade Setup")]
        public BuildingUpgradeData upgradeData;
        public BuildingInventory inventory;

        public List<GameObject> visuals;

        [SerializeField]
        private int currentLevel = 0;

        private void OnEnable()
        {
            inventory = GetComponentInChildren<BuildingInventory>();
            inventory.GetPort("Upgrade").OnItemAdded += TryProcessUpgrade;
        }
        
        private void OnDisable()
        {
            var inventory = GetComponentInChildren<BuildingInventory>();
            inventory.GetPort("Upgrade").OnItemAdded -= TryProcessUpgrade;
        }
        
        public bool IsUpgradeMaterial(ItemDefinition item)
        {
            if (item == null || upgradeData == null)
                return false;

            var level = upgradeData.GetLevel(currentLevel);
            if (level == null)
                return false;

            return item == level.requiredMaterial;
        }

        private BuildingInventoryPort FindUpgradePort()
        {
            var inventory = GetComponentInChildren<BuildingInventory>();
            var ports = inventory.GetPort("Upgrade");
            
            return ports;
        }

        private void TryProcessUpgrade(ItemDefinition item, float amount)
        {
            var level = upgradeData.GetLevel(currentLevel);
            
            float stored = inventory.GetPort("Upgrade").GetItemAmount(level.requiredMaterial);

            if (stored >= level.amountRequired)
            {
                // Consume materials
                inventory.GetPort("Upgrade").Remove(level.requiredMaterial, level.amountRequired);

                // Apply upgrade
                ApplyUpgrade(level);

                currentLevel++;
                
                ApplyVisualLevel(currentLevel);
                Debug.Log($"{name} upgraded to level {currentLevel}");
            }
        }

        private void ApplyUpgrade(BuildingUpgradeDefinition def)
        {
            // Let building handle the upgrade
            SendMessage(
                "OnBuildingUpgrade",
                def,
                SendMessageOptions.DontRequireReceiver
            );
        }
        
        void ApplyVisualLevel(int level)
        {
            for (int i = 0; i < visuals.Count; i++)
                visuals[i].SetActive(i == level);
        }
    }
}
