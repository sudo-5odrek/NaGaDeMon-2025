using UnityEngine;

public enum LootType { Gold, Ammo, Energy, Material }

public class LootPickup : MonoBehaviour
{
    public LootType lootType;
    public int amount = 1;
}

