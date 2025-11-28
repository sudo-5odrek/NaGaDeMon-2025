using NaGaDeMon.Features.Building.Turrets.Bullets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NaGaDeMon.Features.Player
{
    public class PlayerAttack : MonoBehaviour
    {
        public enum AttackMode { Manual, Auto }

        [Header("Mode")]
        public AttackMode mode = AttackMode.Manual;

        [Header("Bullet Behavior")]
        [Tooltip("BulletEffects defines speed, lifetime, prefab, and stackable effects.")]
        public BulletEffects bulletEffects;

        [Header("Targeting")]
        public float maxRange = 4f;
        public LayerMask enemyMask;
        public LayerMask obstacleMask;

        [Header("References")]
        public Transform firePoint;
        public Transform playerRoot;

        [Header("Shooting")]
        public float attackCooldown = 0.8f;

        private InputSystem_Actions input;
        private Camera cam;
        private float lastAttackTime;
        private Collider2D currentTarget;

        private Vector3 PlayerPos => playerRoot ? playerRoot.position : transform.position;

        // --------------------------------------------------------------------
        // INITIALIZATION
        // --------------------------------------------------------------------

        private void Awake()
        {
            cam = Camera.main;
            input = InputContextManager.Instance.input;

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
            if (bulletEffects == null)
                return;

            if (mode == AttackMode.Auto)
                AutoAttack();
            else
                RotateTowardMouse();
        }

        // --------------------------------------------------------------------
        // MANUAL ATTACK
        // --------------------------------------------------------------------

        private void OnAttackInput(InputAction.CallbackContext ctx)
        {
            if (mode != AttackMode.Manual) return;
            if (Time.time - lastAttackTime < attackCooldown) return;

            if (bulletEffects == null)
            {
                Debug.LogWarning("PlayerAttack: No BulletEffects assigned!");
                return;
            }

            Vector3 mouseWorld = GetMouseWorldPosition();
            Vector3 direction = (mouseWorld - firePoint.position).normalized;

            FireProjectile(direction);
        }

        private void RotateTowardMouse()
        {
            Vector3 mouseWorld = GetMouseWorldPosition();
            Vector3 dir = (mouseWorld - PlayerPos).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }

        // --------------------------------------------------------------------
        // AUTO ATTACK
        // --------------------------------------------------------------------

        private void AutoAttack()
        {
            if (Time.time - lastAttackTime < attackCooldown) return;

            // Find closest enemy
            Collider2D[] hits = Physics2D.OverlapCircleAll(PlayerPos, maxRange, enemyMask);
            if (hits.Length == 0)
            {
                currentTarget = null;
                return;
            }

            float bestDist = float.PositiveInfinity;
            Collider2D best = null;

            foreach (var h in hits)
            {
                // LOS check
                Vector2 dir = (h.transform.position - PlayerPos);
                float dist = dir.magnitude;

                if (Physics2D.Raycast(PlayerPos, dir.normalized, dist, obstacleMask))
                    continue;

                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = h;
                }
            }

            currentTarget = best;

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
                    bulletEffects.speed
                );

                Vector2 direction = (predictedPos - shooterPos).normalized;

                FireProjectile(direction);
            }
        }

        // --------------------------------------------------------------------
        // SHOOTING
        // --------------------------------------------------------------------

        private void FireProjectile(Vector3 direction)
        {
            lastAttackTime = Time.time;

            if (bulletEffects.bulletPrefab == null)
            {
                Debug.LogWarning("BulletEffects has no prefab assigned!");
                return;
            }

            GameObject bulletObj = Instantiate(
                bulletEffects.bulletPrefab,
                firePoint.position,
                Quaternion.identity
            );

            if (bulletObj.TryGetComponent<Bullet>(out var bullet))
                bullet.Initialize(bulletEffects, direction);
        }

        // --------------------------------------------------------------------
        // UTILS
        // --------------------------------------------------------------------

        private Vector3 GetMouseWorldPosition()
        {
            Vector2 screenPos = input.Player.Point.ReadValue<Vector2>();

            if (screenPos == Vector2.zero && Mouse.current != null)
                screenPos = Mouse.current.position.ReadValue();

            float dist = Mathf.Abs(cam.transform.position.z);
            Vector3 world = cam.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, dist)
            );

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
