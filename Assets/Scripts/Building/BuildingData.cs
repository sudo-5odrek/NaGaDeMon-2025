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

    public enum PlacementMode
    {
        Single,  // Click to place (default)
        Drag     // Click + drag to place multiple
    }

    [CreateAssetMenu(fileName = "NewBuilding", menuName = "TD/Building")]
    public class BuildingData : ScriptableObject
    {
        [Header("Visuals")]
        public string buildingName;
        public Sprite icon;
        public GameObject prefab;

        [Header("Economy")]
        public List<BuildingCost> cost = new List<BuildingCost>();

        // 🔹 Optional: assign a ScriptableObject that implements IBuildPlacementLogic
        public ScriptableObject placementLogic;

        // Convenience accessor
        public IBuildPlacementLogic GetPlacementLogic()
        {
            return placementLogic as IBuildPlacementLogic;
        }
    }
}