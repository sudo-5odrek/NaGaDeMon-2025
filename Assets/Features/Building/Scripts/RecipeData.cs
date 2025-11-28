using UnityEngine;
using System.Collections.Generic;
using NaGaDeMon.Features.Inventory;

namespace NaGaDeMon.Features.Building
{
    /// <summary>
    /// Defines a crafting recipe: what resources it consumes and what it produces.
    /// Used by ProductionBuilding.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Production/Recipe", fileName = "NewRecipe")]
    public class RecipeData : ScriptableObject
    {
        [Header("General Info")]
        [Tooltip("Display name for the recipe.")]
        public string recipeName = "New Recipe";

        [Tooltip("Time (in seconds) to complete one craft cycle.")]
        public float craftTime = 3f;

        [Header("Inputs")]
        [Tooltip("Resources consumed per craft cycle.")]
        public List<RecipeEntry> inputs = new();

        [Header("Outputs")]
        [Tooltip("Resources produced per craft cycle.")]
        public List<RecipeEntry> outputs = new();
    }

    /// <summary>
    /// Defines a single resource and quantity in a recipe.
    /// </summary>
    [System.Serializable]
    public class RecipeEntry
    {
        [Tooltip("Reference to the ItemDefinition ScriptableObject representing this resource.")]
        public ItemDefinition itemDefinition;

        [Tooltip("Amount consumed or produced per craft cycle.")]
        public float amount = 1f;

        /// <summary>
        /// Returns the unique resource ID (from the ItemDefinition).
        /// </summary>
        public string ResourceId => itemDefinition != null ? itemDefinition.itemID : string.Empty;
    }
}