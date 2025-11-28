using System.Collections.Generic;
using UnityEngine;

namespace Features.Building.Scripts.Upgrade
{
    [CreateAssetMenu(menuName = "Buildings/Upgrade/Upgrade Data")]
    public class BuildingUpgradeData : ScriptableObject
    {
        public List<BuildingUpgradeDefinition> levels = new List<BuildingUpgradeDefinition>();

        public int MaxLevel => levels?.Count ?? 0;

        public BuildingUpgradeDefinition GetLevel(int level)
        {
            if (level < 0 || level >= MaxLevel)
                return null;

            return levels[level];
        }
    }
}