using Player;
using UnityEngine;
using TMPro;

public class InventoryCounterUI : MonoBehaviour
{
    public LootType displayType = LootType.Gold;
    public TextMeshProUGUI text;

    void OnEnable()
    {
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnInventoryChanged += UpdateDisplay;

        UpdateDisplay(); // update immediately on enable
    }

    void OnDisable()
    {
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnInventoryChanged -= UpdateDisplay;
    }

    void UpdateDisplay()
    {
        if (!PlayerInventory.Instance) return;

        var inv = PlayerInventory.Instance;

        int value = displayType switch
        {
            //LootType.Gold => inv.gold,
            //LootType.Ammo => inv.ammo,
            //LootType.Energy => inv.energy,
            //LootType.Material => inv.materials,
            _ => 0
        };

        text.text = value.ToString();
    }
}