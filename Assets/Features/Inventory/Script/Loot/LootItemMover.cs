using NaGaDeMon.Features.Inventory;
using NaGaDeMon.Features.Player;
using UnityEngine;

namespace NaGaDeMon.Features.Inventory.Loot
{
    [RequireComponent(typeof(Collider2D))]
    public class LootItemMover : MonoBehaviour
    {
        [Header("Flight Settings")]
        public float flySmooth = 10f;           // How smoothly loot moves toward player
        public float collectDistance = 0.3f;    // For visual polish before pickup

        [Header("Item Data")]
        public ItemDefinition itemDefinition;   // 🔗 Reference to the item definition
        public float amount = 1f;               // Quantity (optional for stackables)

        private Transform target;
        private float attractSpeed;
        private bool isAttracted;

        // ------------------------------------------------------------
        // MOVEMENT LOGIC
        // ------------------------------------------------------------
        private void Update()
        {
            if (!isAttracted || !target) return;

            Vector3 dir = target.position - transform.position;
            float dist = dir.magnitude;

            // Move smoothly toward the player
            transform.position += dir.normalized * attractSpeed * Time.deltaTime;

            // Optional: spin or scale animation
            transform.Rotate(Vector3.forward * 360f * Time.deltaTime);

            // Collect when close enough
            if (dist <= collectDistance)
                Collect();
        }

        public void AttractTo(Transform player, float force)
        {
            target = player;
            attractSpeed = force;
            isAttracted = true;
        }

        // ------------------------------------------------------------
        // COLLECTION LOGIC
        // ------------------------------------------------------------
        private void Collect()
        {
            if (PlayerInventory.Instance == null)
            {
                Debug.LogError("❌ PlayerInventory.Instance is null!");
                return;
            }

            if (itemDefinition == null)
            {
                Debug.LogError($"❌ LootItem '{gameObject.name}' missing ItemDefinition!");
                return;
            }

            // ✅ Add item to player inventory using new item system
            PlayerInventory.Instance.AddItem(itemDefinition, amount);

            // Optional: play pickup VFX or sound
            // VFXManager.Play("pickup_glow", transform.position);

            Destroy(gameObject);
        }
    }
}
