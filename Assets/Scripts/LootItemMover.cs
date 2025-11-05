using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LootItemMover : MonoBehaviour
{
    [Header("Flight Settings")]
    public float flySmooth = 10f;      // how smoothly loot moves toward player
    public float collectDistance = 0.3f; // for visual polish before pickup

    Transform target;
    float attractSpeed;
    bool isAttracted;

    void Update()
    {
        if (!isAttracted || !target) return;

        Vector3 dir = target.position - transform.position;
        float dist = dir.magnitude;

        // Move toward the player smoothly
        transform.position += dir.normalized * attractSpeed * Time.deltaTime;

        // Optional: small scale or spin animation
        transform.Rotate(Vector3.forward * 360f * Time.deltaTime);

        // Once we're very close, call Collect
        if (dist <= collectDistance)
        {
            Collect();
        }
    }

    public void AttractTo(Transform player, float force)
    {
        target = player;
        attractSpeed = force;
        isAttracted = true;
    }

    void Collect()
    {
        if (PlayerInventory.Instance == null)
        {
            Debug.LogError("❌ PlayerInventory.Instance is null!");
            return;
        }

        if (!TryGetComponent<LootPickup>(out var lootData))
        {
            Debug.LogError("❌ LootPickup not found on loot object!");
            return;
        }

        PlayerInventory.Instance.AddLoot(lootData.lootType, lootData.amount);
        Debug.Log($"✅ Collected {lootData.lootType} x{lootData.amount}");
        Destroy(gameObject);
    }
}