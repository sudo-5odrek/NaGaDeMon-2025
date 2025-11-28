using System.Collections.Generic;
using Features.Inventory.Scripts;
using UnityEngine;

namespace Features.Building.Scripts.Turrets.Bullets
{
    [CreateAssetMenu(menuName = "Game/Combat/Bullet Effects/Database")]
    public class BulletEffectDatabase : ScriptableObject
    {
        [System.Serializable]
        public struct MaterialToEffect
        {
            public ItemDefinition material;     // the ammo item
            public BulletEffects effects;       // the bullet behavior SO
        }

        public List<MaterialToEffect> mappings = new List<MaterialToEffect>();

        /// <summary>
        /// Returns the BulletEffects data for a given material.
        /// If no match is found, returns null.
        /// </summary>
        public BulletEffects GetEffects(ItemDefinition material)
        {
            foreach (var entry in mappings)
            {
                if (entry.material == material)
                    return entry.effects;
            }

            return null;
        }
    }
}