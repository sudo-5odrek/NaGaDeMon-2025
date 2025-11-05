using UnityEngine;
using System.Collections.Generic;

public class Turret : MonoBehaviour
{
    [Header("Targeting")]
    public float detectionRadius = 6f;
    public LayerMask enemyMask;
    public float rotationSpeed = 5f;
    public GameObject head;

    [Header("Firing")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 1f;      // seconds between shots
    public float bulletSpeed = 10f;
    public int damagePerShot = 1;
    public int maxRange;

    [Header("Ammo & Health")]
    public int maxAmmo = 30;
    public int currentAmmo;
    public int maxHP = 100;
    public int currentHP;
    
    public LayerMask obstacleMask;

    float fireTimer;
    Transform currentTarget;

    void Awake()
    {
        currentAmmo = maxAmmo;
        currentHP = maxHP;
    }

    void Update()
    {
        // ✅ Drop target if destroyed, out of range, or LoS broken
        if (currentTarget == null 
            || !TargetInRange(currentTarget)
            || !HasLineOfSight(currentTarget))
        {
            currentTarget = FindClosestEnemy();
        }

        // ✅ Rotate toward target and shoot
        if (currentTarget)
        {
            RotateTowardTarget();
            TryShoot();
        }
    }

    bool HasLineOfSight(Transform target)
    {
        if (target == null) return false;

        Vector2 dir = target.position - transform.position;
        float dist = dir.magnitude;

        // Returns true if there’s no blocking obstacle
        return !Physics2D.Raycast(transform.position, dir.normalized, dist, obstacleMask);
    }

    // === Targeting ===
    Transform FindClosestEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, enemyMask);
        float bestDist = float.PositiveInfinity;
        Transform bestTarget = null;

        foreach (var h in hits)
        {
            if (!HasLineOfSight(h.transform))
                continue; // skip targets behind walls

            float dist = Vector2.Distance(transform.position, h.transform.position);
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
        return Vector2.Distance(transform.position, t.position) <= detectionRadius;
    }

    void RotateTowardTarget()
    {
        Vector2 dir = currentTarget.position - transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion targetRot = Quaternion.Euler(0, 0, angle - 90); // adjust for sprite orientation
        head.transform.rotation = Quaternion.Lerp(head.transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
    }

    // === Shooting ===
    void TryShoot()
    {
        if (currentAmmo <= 0) return;

        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            fireTimer = fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        if (!bulletPrefab || !firePoint || !currentTarget) return;
        if (currentTarget.TryGetComponent<Rigidbody2D>(out var targetRb))
        {
            Vector2 shooterPos = firePoint.position;
            Vector2 targetPos = currentTarget.position;
            Vector2 targetVel = targetRb.linearVelocity;

            // Predict where to aim
            Vector2 predictedPos = TargetPrediction.PredictAimPosition(shooterPos, targetPos, targetVel, bulletSpeed);

            // Aim direction toward future position
            Vector2 aimDir = (predictedPos - shooterPos).normalized;

            // Create and launch bullet
            GameObject bulletObj = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

            if (bulletObj.TryGetComponent<Bullet>(out var bullet))
                bullet.Init(aimDir, bulletSpeed, damagePerShot, maxRange);

            currentAmmo--;
        }
    }

    // === Health ===
    public void TakeDamage(int amount)
    {
        currentHP -= amount;
        Debug.Log($"{name} took {amount} damage. HP: {currentHP}");
        if (currentHP <= 0)
            Destroy(gameObject);
    }

    // === Debug Gizmos ===
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
