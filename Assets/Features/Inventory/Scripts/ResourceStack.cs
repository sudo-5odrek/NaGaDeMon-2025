namespace Features.Inventory.Scripts
{
    [System.Serializable]
    public struct ResourceStack
    {
        public string resourceId;   // "IronOre", "CopperIngot", etc.
        public int amount;
    }
}