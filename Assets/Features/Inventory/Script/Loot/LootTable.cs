using UnityEngine;

[System.Serializable]
public class LootItem
{
    public GameObject itemPrefab;
    [Range(0f, 1f)] public float dropChance = 0.5f;
    public int minAmount = 1;
    public int maxAmount = 1;
}

public class LootTable : MonoBehaviour
{
    [Header("Loot Table")]
    public LootItem[] lootItems;

    public void DropLoot()
    {
        foreach (var loot in lootItems)
        {
            float roll = Random.value;
            if (roll <= loot.dropChance)
            {
                int count = Random.Range(loot.minAmount, loot.maxAmount + 1);
                for (int i = 0; i < count; i++)
                {
                    // small random offset so multiple items don’t stack exactly
                    Vector3 dropPos = transform.position + (Vector3)Random.insideUnitCircle * 0.3f;
                    Instantiate(loot.itemPrefab, dropPos, Quaternion.identity);
                }
            }
        }
    }
}

