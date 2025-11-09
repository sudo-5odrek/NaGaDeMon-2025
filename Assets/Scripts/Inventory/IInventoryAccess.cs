using UnityEngine;

public interface IInventoryAccess
{
    bool CanAccept(string resourceId, float amount = 1f);
    bool CanProvide(string resourceId, float amount = 1f);
    float Add(string resourceId, float amount);
    float Remove(string resourceId, float amount);
    bool IsFull { get; }
    bool IsEmpty { get; }
}

