using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerAttack : MonoBehaviour
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
        public LayerMask obstacleMask;

        [Header("References")]
        public Transform firePoint; // where projectiles originate
        public GameObject projectilePrefab;
        [Tooltip("If left empty, will auto-assign the topmost parent (player root).")]
        public Transform playerRoot;

        private InputSystem_Actions input;
        private Camera cam;
        private float lastAttackTime;
        private Collider2D currentTarget;

        private Vector3 PlayerPos => playerRoot ? playerRoot.position : transform.position;

        private void Awake()
        {
            cam = Camera.main;
            input = InputContextManager.Instance.input;

            // Automatically assign root if not set manually
            if (!playerRoot)
                playerRoot = transform.root;
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
                RotateTowardMouse();
            }
        }

        // ------------------------------------------------------
        // Manual Attack
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
            Vector3 dir = (mouseWorld - PlayerPos).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // Rotate the *attack controller* child, not the player root
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }

        // ------------------------------------------------------
        // Auto Attack
        // ------------------------------------------------------

        private void AutoAttack()
        {
            if (Time.time - lastAttackTime < attackCooldown) return;

            // Find the closest enemy in range
            Collider2D[] hits = Physics2D.OverlapCircleAll(PlayerPos, maxRange, enemyMask);
            if (hits.Length == 0)
            {
                currentTarget = null;
                return;
            }

            float minDist = Mathf.Infinity;
            Collider2D closest = null;

            foreach (var h in hits)
            {
                Vector2 dir = (h.transform.position - PlayerPos);
                float dist = dir.magnitude;

                // ✅ Line of sight check from player root
                bool blocked = Physics2D.Raycast(PlayerPos, dir.normalized, dist, obstacleMask);
                if (blocked)
                    continue;

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

                Vector2 targetVel = Vector2.zero;
                if (currentTarget.TryGetComponent<Rigidbody2D>(out var rbT))
                    targetVel = rbT.linearVelocity;

                Vector2 predictedPos = TargetPrediction.PredictAimPosition(
                    shooterPos,
                    targetPos,
                    targetVel,
                    bulletSpeed
                );

                FireProjectile(predictedPos - (Vector2)firePoint.position);
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
                bullet.Init(direction, bulletSpeed, damagePerShot, maxRange);
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
                Gizmos.DrawWireSphere(PlayerPos, maxRange);
            }
        }
    }
}
