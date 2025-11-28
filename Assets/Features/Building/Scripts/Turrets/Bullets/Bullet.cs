using Features.Building.Scripts.Turrets.Bullets.Effect_Library;
using UnityEngine;

namespace NaGaDeMon.Features.Building.Turrets.Bullets
{
    public class Bullet : MonoBehaviour
    {
        public BulletEffects bulletEffects;

        private Vector3 direction;
        private float timer;

        public void Initialize(BulletEffects effects, Vector3 shootDirection)
        {
            bulletEffects = effects;
            direction = shootDirection.normalized;
        }

        private void Update()
        {
            // Movement
            transform.position += direction * bulletEffects.speed * Time.deltaTime;

            // Lifetime
            timer += Time.deltaTime;
            if (timer >= bulletEffects.lifetime)
                Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            GameObject enemyGO = col.gameObject;

            // Only act on enemies (you can change the layer check however you want)
            if (!enemyGO.CompareTag("Enemy"))
                return;

            Vector3 hitPoint = transform.position;

            // Apply all effects
            foreach (var effect in bulletEffects.effects)
                effect.ApplyEffect(enemyGO, hitPoint);

            Destroy(gameObject);
        }
        private void OnDrawGizmosSelected()
        {
            if (bulletEffects != null)
            {
                foreach (var effect in bulletEffects.effects)
                {
                    if (effect is SplashDamageEffect splash)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireSphere(transform.position, splash.radius);
                    }
                }
            }
        }
    }
    
    
}