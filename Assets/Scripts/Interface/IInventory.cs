namespace Interface
{
    public interface IInventory
    {
        bool HasAnyOutputResource();      // for buildings that produce items
        bool TryAdd(string resourceId, float amount);   // when an item arrives
        bool TryRemove(string resourceId, float amount); // when sending an item
        bool HasFreeSpace(); 
    }
}