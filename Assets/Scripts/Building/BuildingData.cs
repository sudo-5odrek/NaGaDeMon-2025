using System.Collections.Generic;
using Inventory;
using UnityEngine;

namespace Building
{
    [System.Serializable]
    public struct BuildingCost
    {
        public ItemDefinition item;
        public int amount;
    }
    
    [CreateAssetMenu(fileName = "NewBuilding", menuName = "Game/Buildings/Building")]
    public class BuildingData : ScriptableObject
    {
        [Header("Visuals")]
        public string buildingName;
        public Sprite icon;
        public GameObject prefab;

        [Header("Economy")]
        public List<BuildingCost> cost = new List<BuildingCost>();

        [Header("Placement Logic")]
        public ScriptableObject placementLogic;

        // 🆕 NEW — Item acceptance rules
        [Header("Item Rules")]
        public bool acceptAllItems = true;

        [Tooltip("If acceptAllItems is false, the building can only accept these item types.")]
        public List<ItemDefinition> itemWhitelist = new List<ItemDefinition>();

        public IBuildPlacementLogic GetPlacementLogic()
        {
            return placementLogic as IBuildPlacementLogic;
        }
    }
}