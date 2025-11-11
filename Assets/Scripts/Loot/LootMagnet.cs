using Loot;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LootMagnet : MonoBehaviour
{
    [Header("Magnet Settings")]
    public float attractForce = 8f; // how fast loot flies toward player

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<LootItemMover>(out var loot))
        {
            loot.AttractTo(transform, attractForce);
        }
    }
}