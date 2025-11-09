using System;
using UnityEngine;

[Serializable]
public class InventoryPort : IInventoryAccess
{
    public string portName = "Port";
    public bool isInput = true;
    public string[] acceptedResources;
    public float maxCapacity = 10f;
    public float transferRate = 1f;

    [NonSerialized] public Inventory inventory;

    public void Init() => inventory = new Inventory(maxCapacity);

    public bool Accepts(string resourceId)
    {
        if (acceptedResources == null || acceptedResources.Length == 0) return true;
        foreach (var r in acceptedResources)
            if (r == resourceId) return true;
        return false;
    }

    // Interface implementation
    public bool CanAccept(string resourceId, float amount = 1f)
        => isInput && Accepts(resourceId) && inventory.HasFreeSpace();
    public bool CanProvide(string resourceId, float amount = 1f)
        => !isInput && Accepts(resourceId) && inventory.Contains(resourceId, amount);
    public float Add(string resourceId, float amount)
        => Accepts(resourceId) ? inventory.Add(resourceId, amount) : 0f;
    public float Remove(string resourceId, float amount)
        => inventory.Remove(resourceId, amount);
    public bool IsFull => !inventory.HasFreeSpace();
    public bool IsEmpty => inventory.IsEmpty();
}
