using System;
using Player;
using UnityEngine;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    public LootType displayType = LootType.Gold;
    public TextMeshProUGUI text;
    public TextMeshProUGUI contextText;

    private void OnEnable()
    {
        // Subscribe to player inventory updates
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnInventoryChanged += UpdateDisplay;

        // Subscribe to context changes
        if (InputContextManager.Instance != null)
            InputContextManager.Instance.OnContextChange += UpdateContext;

        // Update immediately
        UpdateDisplay();
        UpdateContext();
    }

    private void OnDisable()
    {
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnInventoryChanged -= UpdateDisplay;

        if (InputContextManager.Instance != null)
            InputContextManager.Instance.OnContextChange -= UpdateContext;
    }

    private void UpdateDisplay()
    {
        if (!PlayerInventory.Instance) return;

        var inv = PlayerInventory.Instance;

        int value = displayType switch
        {
            // Uncomment once your PlayerInventory exposes these
            // LootType.Gold => inv.gold,
            // LootType.Ammo => inv.ammo,
            // LootType.Energy => inv.energy,
            // LootType.Material => inv.materials,
            _ => 0
        };

        text.text = value.ToString();
    }

    private void UpdateContext()
    {
        if (InputContextManager.Instance == null || contextText == null)
            return;

        string modeLabel = InputContextManager.Instance.CurrentMode switch
        {
            InputContextManager.InputMode.Normal => "",
            InputContextManager.InputMode.Build => "Building Mode",
            InputContextManager.InputMode.Connect => "Connection Mode",
            _ => "Unknown Mode"
        };

        contextText.text = modeLabel;
    }
}