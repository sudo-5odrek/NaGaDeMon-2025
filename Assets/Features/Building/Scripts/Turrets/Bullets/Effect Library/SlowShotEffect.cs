using Features.Enemies.Scripts.Enemy;
using NaGaDeMon.Features.Building.Turrets.Bullets;
using UnityEngine;

namespace Features.Building.Scripts.Turrets.Bullets.Effect_Library
{
    [CreateAssetMenu(menuName = "Game/Combat/Bullet Effects/Slow Shot")]
    public class SlowShotEffect : BulletEffect
    {
        [Header("Direct Hit")]
        public int damage = 10;

        [Header("Slow Zone Settings")]
        public GameObject slowZonePrefab;
        public float zoneLifetime = 3f;

        [Tooltip("Radius of the slow zone in world units.")]
        public float zoneRadius = 2f;

        public override void ApplyEffect(GameObject target, Vector3 hitPosition)
        {
            // Damage
            if (target.TryGetComponent<EnemyHealth>(out var hp))
                hp.TakeDamage(damage);

            // Spawn slow zone
            if (slowZonePrefab != null)
            {
                var zoneObj = Instantiate(slowZonePrefab, hitPosition, Quaternion.identity);

                // Pass radius to the instance
                if (zoneObj.TryGetComponent<SlowZone>(out var zone))
                    zone.Setup(zoneRadius, zoneLifetime);
            }
        }
    }
}
