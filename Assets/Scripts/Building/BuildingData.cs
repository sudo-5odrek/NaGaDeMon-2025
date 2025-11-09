using UnityEngine;

namespace Building
{
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
        public int cost = 0;

        [Header("Placement")]
        public PlacementMode placementMode = PlacementMode.Single;

        // 🔹 Optional: assign a ScriptableObject that implements IBuildPlacementLogic
        public ScriptableObject placementLogic;

        // Convenience accessor
        public IBuildPlacementLogic GetPlacementLogic()
        {
            return placementLogic as IBuildPlacementLogic;
        }
    }
}