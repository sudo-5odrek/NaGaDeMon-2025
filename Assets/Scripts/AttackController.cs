using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class AttackController : MonoBehaviour
{
    public enum AttackMode { Manual, Auto }
    [Header("Attack Settings")]
    public AttackMode mode = AttackMode.Manual;
    public float bulletSpeed = 5f;

    [Header("Stats")]
    public float maxRange = 4f;
    public float attackCooldown = 0.8f;
    public int damagePerShot = 5;
    public LayerMask enemyMask;
    public Transform firePoint; // where projectiles originate
    public GameObject projectilePrefab;
    
    public LayerMask obstacleMask;

    private InputSystem_Actions input;
    private Camera cam;
    private float lastAttackTime;
    private Collider2D currentTarget;
    

    private void Awake()
    {
        cam = Camera.main;
        input = InputContextManager.Instance.input;
    }

    private void OnEnable()
    {
        input.Player.Attack.performed += OnAttackInput;
    }

    private void OnDisable()
    {
        input.Player.Attack.performed -= OnAttackInput;
    }

    private void Update()
    {
        if (mode == AttackMode.Auto)
        {
            AutoAttack();
        }
        else
        {
            // Manual aim cursor feedback, optional
            RotateTowardMouse();
        }
    }

    // ------------------------------------------------------
    // Manual Attack (Mode 1)
    // ------------------------------------------------------

    private void OnAttackInput(InputAction.CallbackContext ctx)
    {
        if (mode != AttackMode.Manual) return;
        if (Time.time - lastAttackTime < attackCooldown) return;

        Vector3 mouseWorld = GetMouseWorldPosition();
        FireProjectile(mouseWorld - firePoint.position);
    }

    private void RotateTowardMouse()
    {
        Vector3 mouseWorld = GetMouseWorldPosition();
        Vector3 dir = (mouseWorld - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
    }

    // ------------------------------------------------------
    // Auto Attack (Mode 2)
    // ------------------------------------------------------

    private void AutoAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;

        // Find the closest enemy in range
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, maxRange, enemyMask);
        if (hits.Length == 0)
        {
            currentTarget = null;
            return;
        }

        // --- Find closest visible enemy ---
        float minDist = Mathf.Infinity;
        Collider2D closest = null;

        foreach (var h in hits)
        {
            Vector2 dir = h.transform.position - transform.position;
            float dist = dir.magnitude;

            // ✅ Perform line-of-sight check
            bool blocked = Physics2D.Raycast(transform.position, dir.normalized, dist, obstacleMask);
            if (blocked)
                continue; // skip targets behind walls

            // ✅ If visible and closer, pick this target
            if (dist < minDist)
            {
                minDist = dist;
                closest = h;
            }
        }

        currentTarget = closest;
        
        if (currentTarget)
        {
            Vector2 shooterPos = firePoint.position;
            Vector2 targetPos = currentTarget.transform.position;
            Vector2 targetVel = currentTarget.TryGetComponent<Rigidbody2D>(out var rbT) ? rbT.linearVelocity : Vector2.zero;
            Vector2 predictedPos = TargetPrediction.PredictAimPosition(shooterPos, targetPos, targetVel, bulletSpeed);
            FireProjectile(currentTarget.transform.position - firePoint.position);
        }
    }

    // ------------------------------------------------------
    // Shared Utilities
    // ------------------------------------------------------

    private void FireProjectile(Vector3 direction)
    {
        lastAttackTime = Time.time;
        direction.Normalize();

        GameObject bulletObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        if (bulletObj.TryGetComponent<Bullet>(out var bullet))
            bullet.Init(direction, bulletSpeed, damagePerShot, maxRange); // or whatever speed you prefer
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector2 screenPos = input.Player.Point.ReadValue<Vector2>();
        if (screenPos == Vector2.zero && Mouse.current != null)
            screenPos = Mouse.current.position.ReadValue();

        float distance = Mathf.Abs(cam.transform.position.z);
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, distance));
        world.z = 0f;
        return world;
    }

    private void OnDrawGizmosSelected()
    {
        if (mode == AttackMode.Auto)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, maxRange);
        }
    }
}

