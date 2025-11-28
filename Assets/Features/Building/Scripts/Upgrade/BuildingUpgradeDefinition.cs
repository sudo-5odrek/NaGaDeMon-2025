using NaGaDeMon.Features.Inventory;
using UnityEngine;

namespace NaGaDeMon.Features.Building.Upgrade
{
    [CreateAssetMenu(menuName = "TD/Building Upgrade Definition")]
    public class BuildingUpgradeDefinition : ScriptableObject
    {
        [Header("Cost")]
        public ItemDefinition requiredMaterial;
        public int amountRequired = 1;

        [Header("Generic Upgrade Modifiers")]
        public float modifier1 = 1f;
        public float modifier2 = 1f;
        public float modifier3 = 1f;

        // These are intentionally generic — each building decides what to do with them.
    }
}
