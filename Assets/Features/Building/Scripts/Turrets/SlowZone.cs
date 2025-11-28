using Features.Enemies.Scripts.Enemy;
using UnityEngine;

namespace Features.Building.Scripts.Turrets
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class SlowZone : MonoBehaviour
    {
        [Header("Zone Settings")]
        public float zoneLifetime = 3f;   // NEW: zone controls its own lifetime
        public float slowMultiplier = 0.25f;

        [Header("References")]
        [Tooltip("Visuals root. If null, uses first child.")]
        [SerializeField] private Transform visuals;

        private CircleCollider2D col;

        void Awake()
        {
            col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;

            // Auto-assign visuals if needed
            if (visuals == null && transform.childCount > 0)
                visuals = transform.GetChild(0);
        }

        private void OnEnable()
        {
            // NEW: Zone manages its removal itself
            if (Application.isPlaying)
                StartCoroutine(DestroyAfterLifetime());
        }

        private System.Collections.IEnumerator DestroyAfterLifetime()
        {
            yield return new WaitForSeconds(zoneLifetime);

            // Optional: Fade-out before destroying
            // TODO: call a VFX fade function here if you want

            Destroy(gameObject);
        }

        /// <summary>
        /// Sets gameplay radius and scales visuals accordingly.
        /// </summary>
        public void Setup(float radius, float lifetime)
        {
            // Set collider trigger radius
            col.radius = radius;

            // Visuals scale (diameter)
            if (visuals != null)
            {
                float diameter = radius * 2f;
                visuals.localScale = new Vector3(diameter, diameter, 1f);
            }
            
            zoneLifetime = lifetime;
        }

        // Slow logic
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<EnemyPathMover>(out var mover))
            {
                mover.ApplySpeedModifier(slowMultiplier);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.TryGetComponent<EnemyPathMover>(out var mover))
            {
                mover.RemoveSpeedModifier(slowMultiplier);
            }
        }
    }
}
