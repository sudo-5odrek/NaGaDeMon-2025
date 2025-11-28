using UnityEngine;

namespace NaGaDeMon.Features.Inventory
{
    [System.Serializable]
    public struct ResourceStack
    {
        public string resourceId;   // "IronOre", "CopperIngot", etc.
        public int amount;
    }
}