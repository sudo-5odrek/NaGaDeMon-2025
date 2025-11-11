using UnityEngine;

namespace Inventory
{
    public interface IInventoryAccess
    {
        bool CanAccept(ItemDefinition item, float amount);
        bool CanProvide(ItemDefinition item, float amount);
        float Add(ItemDefinition item, float amount);
        float Remove(ItemDefinition item, float amount);

        bool IsFull { get; }
        bool IsEmpty { get; }
    }
}

