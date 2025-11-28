using UnityEngine;
using Floating_Text_Service;

namespace Enemy
{
    [RequireComponent(typeof(EnemyPathMover))]
    public class EnemyAttack : MonoBehaviour
    {
        public enum EnemyAttackMode
        {
            Impact,        // damage on hit then destroy
            Continuous     // stop and attack over time
        }

        [Header("Attack Settings")]
        public EnemyAttackMode attackMode = EnemyAttackMode.Impact;

        [Header("Continuous Attack")]
        public float attackInterval = 1.0f;
        public int attackDamage = 5;

        [Header("Floating Text")]
        public FloatingTextStyle damageTextStyle;   // assign in inspector

        private float attackTimer;
        private bool isAttacking = false;
        private Nexus targetNexus;

        private EnemyPathMover mover;

        void Awake()
        {
            mover = GetComponent<EnemyPathMover>();
        }

        public void SetAttackMode(EnemyAttackMode mode)
        {
            attackMode = mode;
        }

        private void Update()
        {
            // Only for continuous mode
            if (!isAttacking || attackMode != EnemyAttackMode.Continuous)
                return;

            attackTimer -= Time.deltaTime;

            if (attackTimer <= 0f)
            {
                attackTimer = attackInterval;

                // Deal damage to nexus
                if (targetNexus != null)
                {
                    targetNexus.TakeDamage(attackDamage);
                    SpawnDamageText(attackDamage);
                }
            }
        }

        // Called by EnemyPathMover when Nexus collider is hit
        public void OnReachNexus(Nexus nexus)
        {
            targetNexus = nexus;

            if (attackMode == EnemyAttackMode.Impact)
            {
                // Impact → deal burst damage then die
                nexus.TakeDamage(10);
                SpawnDamageText(10);
                Destroy(gameObject);
            }
            else if (attackMode == EnemyAttackMode.Continuous)
            {
                // Continuous → stop movement, start attack loop
                mover.StopMovement();
                isAttacking = true;
                attackTimer = attackInterval;
            }
        }

        // Spawns floating damage text between enemy and nexus
        private void SpawnDamageText(int dmg)
        {
            if (damageTextStyle == null || targetNexus == null)
                return;

            // Mid-point position between enemy and nexus
            Vector3 midPoint =
                (transform.position + targetNexus.transform.position) * 0.8f;

            FloatingTextData data = new FloatingTextData
            {
                text = "- " + dmg,
                worldPosition = midPoint,
                //color = Color.red
            };

            FloatingTextService.Instance.Spawn(damageTextStyle, data);
        }
    }
}
