using UnityEngine;

namespace Building.Production
{
    [CreateAssetMenu(menuName = "Production/Building")]
    public class ProductionBuildingData : ScriptableObject
    {
        public string buildingName;

        [Header("Storage")]
        public int inputStorageCapacity = 50;
        public int outputStorageCapacity = 50;

        [Header("Recipes")]
        public RecipeData[] availableRecipes;

        [Header("Default Recipe")]
        public RecipeData defaultRecipe;
    }
}