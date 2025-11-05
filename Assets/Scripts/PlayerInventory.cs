using System;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public int gold, ammo, energy, materials;

    public static PlayerInventory Instance { get; private set; }
    
    public event Action OnInventoryChanged;

    private void Awake()
    {
        // Enforce a single instance
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        // Optional: keep between scenes
        // DontDestroyOnLoad(gameObject);
    }

    public void AddLoot(LootType type, int amount)
    {
        switch (type)
        {
            case LootType.Gold: gold += amount; break;
            case LootType.Ammo: ammo += amount; break;
            case LootType.Energy: energy += amount; break;
            case LootType.Material: materials += amount; break;
        }
        
        OnInventoryChanged?.Invoke();
        Debug.Log($"Picked up {amount} {type}");
    }
}

