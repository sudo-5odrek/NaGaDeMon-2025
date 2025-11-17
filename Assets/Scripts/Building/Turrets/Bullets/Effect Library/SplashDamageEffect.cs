using Enemy;
using UnityEngine;

namespace Building.Turrets.Bullets.Effect_Library
{
    [CreateAssetMenu(menuName = "TD/Bullet Effects/Splash Damage")]
    public class SplashDamageEffect : BulletEffect
    {
        [Header("Splash Settings")]
        public float radius = 2f;

        [Header("Damage Settings")]
        public int directHitDamage = 10;
        public bool useBonusDamageForDirectTarget = true;
        public int directBonusDamage = 0;

        public int splashDamage = 5;
        public bool includeDirectTargetInSplash = false;

        [Header("Visual Effect")]
        [Tooltip("A simple sprite prefab with a SpriteRenderer. No animation needed.")]
        public GameObject splashVfxPrefab;

        public LayerMask enemyMask;

        public override void ApplyEffect(GameObject enemyGO, Vector3 hitPoint)
        {
            if (enemyGO == null) return;

            // -------------------------
            // 1. Damage direct target
            // -------------------------
            var health = enemyGO.GetComponent<EnemyHealth>();
            if (health != null)
            {
                int dmg = directHitDamage + (useBonusDamageForDirectTarget ? directBonusDamage : 0);
                health.TakeDamage(dmg);
            }

            // -------------------------
            // 2. Spawn animated VFX
            // -------------------------
            if (splashVfxPrefab != null)
            {
                GameObject vfx = Object.Instantiate(splashVfxPrefab, hitPoint, Quaternion.identity);
                var sr = vfx.GetComponent<SpriteRenderer>();

                if (sr != null)
                    AnimateSplashVFX(vfx, sr);

                // Destroy after effect is finished
                Object.Destroy(vfx, 0.35f);
            }

            // -------------------------
            // 3. Splash damage
            // -------------------------
            Collider2D[] hits = Physics2D.OverlapCircleAll(hitPoint, radius, enemyMask);

            foreach (var hit in hits)
            {
                if (hit.gameObject == enemyGO && !includeDirectTargetInSplash)
                    continue;

                var h = hit.GetComponent<EnemyHealth>();
                if (h != null)
                    h.TakeDamage(splashDamage);
            }
        }

        // --------------------------------------------------------
        // Procedural splash animation — ALWAYS matches radius
        // --------------------------------------------------------
        private void AnimateSplashVFX(GameObject vfx, SpriteRenderer sr)
        {
            vfx.transform.localScale = Vector3.zero;

            vfx.AddComponent<SplashAnimator>().Initialize(sr, radius);
        }
    }
}
