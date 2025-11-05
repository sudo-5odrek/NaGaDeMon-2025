using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Stats")]
    public int maxHP = 20;
    public int currentHP;

    void Awake()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int amount)
    {
        currentHP -= amount;
        Debug.Log($"{name} took {amount} damage. HP: {currentHP}");

        if (currentHP <= 0)
            Die();
    }

    void Die()
    {
        Debug.Log($"{name} died!");
        
        // Drop loot if loot table exists
        if (TryGetComponent<LootTable>(out var lootTable))
            lootTable.DropLoot();
        
        Destroy(gameObject); // later: replace with pooling or death animation
    }
}