using UnityEngine;

namespace NaGaDeMon.Features.Inventory
{
    [CreateAssetMenu(menuName = "Game/Items/Item Definition", fileName = "NewItem")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identification")]
        public string itemID;            // unique string ID (e.g. "iron_ore")
        public string displayName;
        public Sprite icon;

        [Header("Properties")]
        public float weight = 1f;
        public int maxStack = 99;

        [Header("Prefabs")]
        public GameObject worldPrefab;   // for enemy drops / pick-ups
        public GameObject beltPrefab;    // for conveyors
        public GameObject uiPrefab;      // for UI icons, if needed

        [Header("Category / Tags")]
        public ItemType itemType;
    }

    public enum ItemType
    {
        Resource,
        Component,
        Equipment,
        Consumable,
    }
}