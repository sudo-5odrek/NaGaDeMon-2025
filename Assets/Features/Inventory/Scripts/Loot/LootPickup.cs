using UnityEngine;

namespace Features.Inventory.Scripts.Loot
{
    public enum LootType { Gold, Ammo, Energy, Material }

    public class LootPickup : MonoBehaviour
    {
        public string lootType;
        public int amount = 1;
    }

}
