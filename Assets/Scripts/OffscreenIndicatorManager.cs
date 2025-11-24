using System.Collections.Generic;
using UnityEngine;
using Pool_Services;       // Your GenericPool namespace

namespace UI.Indicators
{
    public class OffscreenIndicatorManager : MonoBehaviour
    {
        public static OffscreenIndicatorManager Instance { get; private set; }

        [Header("References")]
        public Camera targetCamera;                 
        public Canvas canvas;                       
        public RectTransform canvasRect;            
        public OffscreenIndicator indicatorPrefab;  

        [Header("Settings")]
        public float minWorldDistance = 3f;          // No indicator if enemy too close
        public float edgePadding = 0.001f;           // Tiny clamp padding for edges

        // ----------------------------------------------------------
        // Internal state
        // ----------------------------------------------------------
        private GenericPool<OffscreenIndicator> indicatorPool;
        private readonly List<Transform> enemies = new();
        private readonly List<OffscreenIndicator> activeIndicators = new();

        private Camera uiCamera; // Only used if ScreenSpaceCamera

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (!targetCamera)
                targetCamera = Camera.main;

            if (!canvas)
                canvas = FindObjectOfType<Canvas>();

            if (!canvasRect)
                canvasRect = canvas.GetComponent<RectTransform>();

            // ScreenSpaceCamera → use canvas camera
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
                uiCamera = canvas.worldCamera;
            else
                uiCamera = null;

            // ------------------------------------------------------
            // Initialize pool
            // ------------------------------------------------------
            indicatorPool = new GenericPool<OffscreenIndicator>(
                indicatorPrefab,
                canvasRect,
                prewarmCount: 30
            );

            // Ensure all pooled objects are disabled on start
            foreach (var inst in indicatorPool.GetAllPooledObjects())
                inst.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            RefreshIndicators();
        }

        // ----------------------------------------------------------
        // Public API
        // ----------------------------------------------------------
        public void RegisterEnemy(Transform enemy)
        {
            if (enemy && !enemies.Contains(enemy))
                enemies.Add(enemy);
        }

        public void UnregisterEnemy(Transform enemy)
        {
            if (enemy)
                enemies.Remove(enemy);
        }

        // ----------------------------------------------------------
        // Main update
        // ----------------------------------------------------------
        private void RefreshIndicators()
        {
            // Disable previous frame’s indicators
            foreach (var ind in activeIndicators)
                indicatorPool.Release(ind);

            activeIndicators.Clear();

            // Process all enemies
            foreach (var enemy in enemies)
            {
                if (!enemy) continue;

                Vector3 worldPos = enemy.position;

                // Too close → ignore
                if ((targetCamera.transform.position - worldPos).sqrMagnitude <
                    minWorldDistance * minWorldDistance)
                    continue;

                // Convert to viewport
                Vector3 vp = targetCamera.WorldToViewportPoint(worldPos);

                // If behind camera → flip to front
                if (vp.z < 0)
                {
                    vp.x = 1f - vp.x;
                    vp.y = 1f - vp.y;
                    vp.z = Mathf.Abs(vp.z);
                }

                // Check if visible
                bool isVisible =
                    vp.x >= 0f && vp.x <= 1f &&
                    vp.y >= 0f && vp.y <= 1f;

                if (isVisible)
                    continue;

                // --------------------------------------------------
                // Compute intersection of ray with screen edges
                // --------------------------------------------------

                Vector2 dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f);

                if (dir == Vector2.zero)
                    continue;

                // Ray intersection t parameters
                float tLeft = (-0.5f) / dir.x;   // viewport x = 0
                float tRight = (0.5f) / dir.x;  // viewport x = 1
                float tBottom = (-0.5f) / dir.y; // viewport y = 0
                float tTop = (0.5f) / dir.y;     // viewport y = 1

                List<float> hits = new();

                if (tLeft > 0) hits.Add(tLeft);
                if (tRight > 0) hits.Add(tRight);
                if (tBottom > 0) hits.Add(tBottom);
                if (tTop > 0) hits.Add(tTop);

                if (hits.Count == 0)
                    continue;

                // Smallest positive intersection
                float tEdge = hits[0];
                for (int i = 1; i < hits.Count; i++)
                {
                    if (hits[i] < tEdge)
                        tEdge = hits[i];
                }

                // Edge point in viewport (clamped)
                Vector2 vpEdge = new Vector2(
                    0.5f + dir.x * tEdge,
                    0.5f + dir.y * tEdge
                );

                vpEdge.x = Mathf.Clamp01(vpEdge.x);
                vpEdge.y = Mathf.Clamp01(vpEdge.y);

                // Tiny padding so we don't overlap the border
                vpEdge.x = Mathf.Clamp(vpEdge.x, edgePadding, 1f - edgePadding);
                vpEdge.y = Mathf.Clamp(vpEdge.y, edgePadding, 1f - edgePadding);

                // Convert to screen space
                Vector3 screenPoint = targetCamera.ViewportToScreenPoint(
                    new Vector3(vpEdge.x, vpEdge.y, vp.z)
                );

                // Convert to canvas local space
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPoint,
                    uiCamera,
                    out Vector2 localPos
                );

                // Compute rotation angle
                float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                // Spawn indicator
                var arrow = indicatorPool.Get();
                arrow.SetPositionAndRotation(localPos, angleDeg);

                activeIndicators.Add(arrow);
            }
        }
    }
}
