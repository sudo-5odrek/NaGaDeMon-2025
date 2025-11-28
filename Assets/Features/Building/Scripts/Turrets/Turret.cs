using Features.Building.Scripts.Building_Inventory;
using Features.Building.Scripts.Turrets.Bullets;
using UnityEngine;

namespace Features.Building.Scripts.Turrets
{
    [RequireComponent(typeof(BuildingInventory))]
    public class Turret : MonoBehaviour
    {
        [Header("Targeting")]
        public LayerMask enemyMask;
        public float rotationSpeed = 5f;
        public GameObject head;
        public LayerMask obstacleMask;

        [Header("Firing")]
        public Transform firePoint;

        [Header("Ammo → Bullet Effects Mapping")]
        public BulletEffectDatabase bulletEffectDatabase;

        [Header("Ammo Behavior")]
        public bool useFallbackIfNoAmmo = false;
        public BulletEffects fallbackBulletEffects;

        [Header("Health")]
        public int maxHP = 100;
        public int currentHP;

        private float fireTimer;
        private Transform currentTarget;
        private BuildingInventory buildingInventory;
        private BuildingInventoryPort ammoPort;

        private BulletEffects currentBulletEffects; 
        private float currentDetectionRange = 0f; // ← computed from bullet reach

        // ----------------------------------------------------------------------
        // INITIALIZATION
        // ----------------------------------------------------------------------

        void Awake()
        {
            currentHP = maxHP;
            buildingInventory = GetComponent<BuildingInventory>();

            ammoPort = buildingInventory.GetInput() as BuildingInventoryPort;
            if (ammoPort == null)
                Debug.LogWarning($"[{name}] No ammo input port found on turret!");
        }

        // ----------------------------------------------------------------------
        // UPDATE LOOP
        // ----------------------------------------------------------------------

        void Update()
        {
            UpdateBulletEffectsFromAmmo();
            UpdateDetectionRange();

            if (currentBulletEffects == null)
                return;

            // Acquire target or refresh
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

        // ----------------------------------------------------------------------
        // LOAD BULLET DATA FROM AMMO / FALLBACK
        // ----------------------------------------------------------------------

        private void UpdateBulletEffectsFromAmmo()
        {
            if (ammoPort == null)
            {
                currentBulletEffects = null;
                return;
            }

            if (!ammoPort.IsEmpty)
            {
                var material = ammoPort.GetCurrentItemDefinition();
                currentBulletEffects = bulletEffectDatabase.GetEffects(material);
                return;
            }

            if (useFallbackIfNoAmmo)
            {
                currentBulletEffects = fallbackBulletEffects;
            }
            else
            {
                currentBulletEffects = null;
            }
        }

        // ----------------------------------------------------------------------
        // AUTO-DETECTION RANGE
        // ----------------------------------------------------------------------

        private void UpdateDetectionRange()
        {
            if (currentBulletEffects == null)
            {
                currentDetectionRange = 0f;
                return;
            }

            // Bullet reach = speed × lifetime
            currentDetectionRange =
                currentBulletEffects.speed * currentBulletEffects.lifetime;
        }

        // ----------------------------------------------------------------------
        // TARGETING + LINE OF SIGHT
        // ----------------------------------------------------------------------

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
            if (currentDetectionRange <= 0f)
                return null;
            
            Vector3 offset = new Vector3(transform.position.x + 0.5f, transform.position.y + 0.5f, transform.position.z);

            Collider2D[] hits =
                Physics2D.OverlapCircleAll(offset, currentDetectionRange, enemyMask);

            float bestDist = float.PositiveInfinity;
            Transform bestTarget = null;

            foreach (var h in hits)
            {
                if (!HasLineOfSight(h.transform))
                    continue;

                float dist = Vector2.Distance(offset, h.transform.position);

                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestTarget = h.transform;
                }
            }

            return bestTarget;
        }

        bool TargetInRange(Transform t)
        {
            if (currentDetectionRange <= 0f) return false;
            return Vector2.Distance(transform.position, t.position) <= currentDetectionRange;
        }

        // ----------------------------------------------------------------------
        // ROTATION + SHOOTING
        // ----------------------------------------------------------------------

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

            if (!useFallbackIfNoAmmo && !HasAmmo())
                return;

            if (HasAmmo())
                ConsumeAmmo();

            fireTimer = currentBulletEffects.fireInterval;
            Shoot();
        }

        void Shoot()
        {
            if (currentBulletEffects == null || currentBulletEffects.bulletPrefab == null)
                return;

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

        // ----------------------------------------------------------------------
        // AMMO
        // ----------------------------------------------------------------------

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

        // ----------------------------------------------------------------------
        // HEALTH
        // ----------------------------------------------------------------------

        public void TakeDamage(int amount)
        {
            currentHP -= amount;
            if (currentHP <= 0)
                Destroy(gameObject);
        }

        // ----------------------------------------------------------------------
        // DEBUG VISUALIZATION
        // ----------------------------------------------------------------------

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 offset = new Vector3(transform.position.x + 0.5f, transform.position.y + 0.5f, transform.position.z);
            Gizmos.DrawWireSphere(offset, currentDetectionRange);
        }
    }
}
