using UnityEngine;

public class Nexus : MonoBehaviour
{
    [Header("Stats")]
    public int maxHP = 100;
    public int currentHP;

    void Awake()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int amount)
    {
        currentHP -= amount;
        currentHP = Mathf.Max(currentHP, 0);
        //Debug.Log($"Nexus HP: {currentHP}/{maxHP}");

        if (currentHP <= 0)
            GameOver();
    }

    void GameOver()
    {
        //Debug.Log("💀 GAME OVER — Nexus destroyed!");
        // later: trigger UI, stop spawns, pause game, etc.
    }
}