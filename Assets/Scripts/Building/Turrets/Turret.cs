using UnityEngine;
using System.Collections.Generic;
using Building;
using Building.Turrets.Bullets;
using Inventory;

namespace Building.Turrets
{
    [RequireComponent(typeof(BuildingInventory))]
    public class Turret : MonoBehaviour
    {
        [Header("Targeting")]
        public float detectionRadius = 6f;
        public LayerMask enemyMask;
        public float rotationSpeed = 5f;
        public GameObject head;
        public LayerMask obstacleMask;

        [Header("Firing")]
        public Transform firePoint;

        [Header("Ammo → Bullet Effects Mapping")]
        public BulletEffectDatabase bulletEffectDatabase;

        [Header("Ammo Behavior")]
        [Tooltip("If enabled, turret fires fallback bullets when no ammo is available.")]
        public bool useFallbackIfNoAmmo = false;
        public BulletEffects fallbackBulletEffects;

        [Header("Health")]
        public int maxHP = 100;
        public int currentHP;

        private float fireTimer;
        private Transform currentTarget;
        private BuildingInventory buildingInventory;
        private BuildingInventoryPort ammoPort;

        private BulletEffects currentBulletEffects; // Loaded from ammo or fallback

        // --------------------------------------------------
        // INITIALIZATION
        // --------------------------------------------------

        void Awake()
        {
            currentHP = maxHP;
            buildingInventory = GetComponent<BuildingInventory>();

            ammoPort = buildingInventory.GetInput() as BuildingInventoryPort;
            if (ammoPort == null)
                Debug.LogWarning($"[{name}] No ammo input port found on turret!");
        }

        // --------------------------------------------------
        // UPDATE LOOP
        // --------------------------------------------------

        void Update()
        {
            UpdateBulletEffectsFromAmmo();

            // If no bullet behavior → cannot shoot
            if (currentBulletEffects == null)
                return;

            // Acquire target
            if (currentTarget == null
                || !TargetInRange(currentTarget)
                || !HasLineOfSight(currentTarget))
            {
                currentTarget = FindClosestEnemy();
            }

            if (currentTarget != null)
            {
                RotateTowardTarget();
                TryShoot();
            }
        }

        // --------------------------------------------------
        // LOAD BULLET DATA FROM AMMO / FALLBACK
        // --------------------------------------------------

        private void UpdateBulletEffectsFromAmmo()
        {
            if (ammoPort == null)
            {
                currentBulletEffects = null;
                return;
            }

            // If we have ammo → use that ammo's bullet effects
            if (!ammoPort.IsEmpty)
            {
                var material = ammoPort.GetCurrentItemDefinition();
                currentBulletEffects = bulletEffectDatabase.GetEffects(material);
                return;
            }

            // If we have NO ammo → check fallback mode
            if (useFallbackIfNoAmmo)
            {
                currentBulletEffects = fallbackBulletEffects;
            }
            else
            {
                currentBulletEffects = null;
            }
        }

        // --------------------------------------------------
        // TARGETING + LINE OF SIGHT
        // --------------------------------------------------

        bool HasLineOfSight(Transform target)
        {
            if (target == null) return false;

            Vector2 dir = target.position - transform.position;
            float dist = dir.magnitude;

            return !Physics2D.Raycast(
                transform.position,
                dir.normalized,
                dist,
                obstacleMask
            );
        }

        Transform FindClosestEnemy()
        {
            Collider2D[] hits =
                Physics2D.OverlapCircleAll(transform.position, detectionRadius, enemyMask);

            float bestDist = float.PositiveInfinity;
            Transform bestTarget = null;

            foreach (var h in hits)
            {
                if (!HasLineOfSight(h.transform))
                    continue;

                float dist = Vector2.Distance(transform.position, h.transform.position);

                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestTarget = h.transform;
                }
            }

            return bestTarget;
        }

        bool TargetInRange(Transform t) =>
            Vector2.Distance(transform.position, t.position) <= detectionRadius;

        // --------------------------------------------------
        // ROTATION + SHOOTING
        // --------------------------------------------------

        void RotateTowardTarget()
        {
            Vector2 dir = currentTarget.position - transform.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            Quaternion targetRot = Quaternion.Euler(0, 0, angle - 90);
            head.transform.rotation = Quaternion.Lerp(
                head.transform.rotation,
                targetRot,
                Time.deltaTime * rotationSpeed
            );
        }

        void TryShoot()
        {
            fireTimer -= Time.deltaTime;
            if (fireTimer > 0f) return;

            // If we require ammo but have none → don't shoot
            if (!useFallbackIfNoAmmo && !HasAmmo())
                return;

            // If we use fallback mode we only consume ammo when available
            if (HasAmmo())
                ConsumeAmmo();

            fireTimer = currentBulletEffects.fireInterval;
            Shoot();
        }

        void Shoot()
        {
            if (currentBulletEffects == null || currentBulletEffects.bulletPrefab == null)
                return;

            // Spawn bullet
            GameObject bulletObj = Instantiate(
                currentBulletEffects.bulletPrefab,
                firePoint.position,
                Quaternion.identity
            );

            if (bulletObj.TryGetComponent<Bullet>(out var bullet))
            {
                Vector2 shooterPos = firePoint.position;
                Vector2 targetPos = currentTarget.position;

                Vector2 dir = (targetPos - shooterPos).normalized;
                bullet.Initialize(currentBulletEffects, dir);
            }
        }

        // --------------------------------------------------
        // AMMO HANDLING
        // --------------------------------------------------

        private bool HasAmmo()
        {
            return ammoPort != null && !ammoPort.IsEmpty;
        }

        private void ConsumeAmmo()
        {
            if (ammoPort == null || ammoPort.IsEmpty)
                return;

            var itemDef = ammoPort.GetCurrentItemDefinition();
            if (itemDef != null)
                ammoPort.Remove(itemDef, 1f);
        }

        // --------------------------------------------------
        // HEALTH
        // --------------------------------------------------

        public void TakeDamage(int amount)
        {
            currentHP -= amount;
            if (currentHP <= 0)
                Destroy(gameObject);
        }

        // --------------------------------------------------
        // DEBUG VISUALIZATION
        // --------------------------------------------------

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
