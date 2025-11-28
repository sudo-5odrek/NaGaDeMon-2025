using UnityEngine;

namespace NaGaDeMon.Features.Building.Production
{
    [CreateAssetMenu(menuName = "Game/Buildings/Production Building")]
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