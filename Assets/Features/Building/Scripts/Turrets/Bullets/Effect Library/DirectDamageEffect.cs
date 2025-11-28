using Enemy;
using UnityEngine;

namespace Building.Turrets.Bullets.Effect_Library
{
    [CreateAssetMenu(menuName = "Game/Combat/Bullet Effects/Direct Damage")]
    public class DirectDamageEffect : BulletEffect
    {
        [Header("Damage Settings")]
        public int damageAmount = 5;

        public override void ApplyEffect(GameObject enemyGO, Vector3 hitPoint)
        {
            if (enemyGO == null) return;

            // Try to find an enemy health component
            var health = enemyGO.GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.TakeDamage(damageAmount);
                return;
            }

            // If there's no EnemyHealth, warn once
            Debug.LogWarning($"DirectDamageEffect: {enemyGO.name} has no EnemyHealth component.");
        }
    }
}