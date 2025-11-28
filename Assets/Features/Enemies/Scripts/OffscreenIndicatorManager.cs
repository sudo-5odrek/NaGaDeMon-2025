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
        public float minWorldDistance = 3f;          // Ignore very close enemies

        [Header("Edge Offset (pixels)")]
        public float edgeOffsetHorizontal = 20f;     // Left/Right inward offset
        public float edgeOffsetVertical = 20f;       // Top/Bottom inward offset

        public float clampPadding = 0.001f;          // Prevent edge jitter

        // ----------------------------------------------------------
        // Internal state
        // ----------------------------------------------------------
        private GenericPool<OffscreenIndicator> indicatorPool;
        private readonly List<Transform> enemies = new();
        private readonly List<OffscreenIndicator> activeIndicators = new();

        private Camera uiCamera; // Canvas camera if ScreenSpaceCamera

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

            // ScreenSpaceCamera uses canvas camera
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

            // Make sure all pooled objects are disabled
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
        // Main update loop
        // ----------------------------------------------------------
        private void RefreshIndicators()
        {
            // Disable previous frame's indicators
            foreach (var ind in activeIndicators)
                indicatorPool.Release(ind);

            activeIndicators.Clear();

            // Process each enemy
            foreach (Transform enemy in enemies)
            {
                if (!enemy) continue;

                Vector3 worldPos = enemy.position;

                // Too close to show indicator
                if ((targetCamera.transform.position - worldPos).sqrMagnitude <
                    minWorldDistance * minWorldDistance)
                    continue;

                // Convert enemy to viewport
                Vector3 vp = targetCamera.WorldToViewportPoint(worldPos);

                // Flip if behind camera
                if (vp.z < 0)
                {
                    vp.x = 1f - vp.x;
                    vp.y = 1f - vp.y;
                    vp.z = Mathf.Abs(vp.z);
                }

                // Skip on-screen enemies
                bool isVisible =
                    vp.x >= 0f && vp.x <= 1f &&
                    vp.y >= 0f && vp.y <= 1f;

                if (isVisible)
                    continue;

                // ------------------------------------------------------
                // Determine intersection with screen edges
                // ------------------------------------------------------

                Vector2 dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
                if (dir == Vector2.zero)
                    continue;

                float tLeft   = (-0.5f) / dir.x;  // x = 0
                float tRight  = (0.5f) / dir.x;   // x = 1
                float tBottom = (-0.5f) / dir.y;  // y = 0
                float tTop    = (0.5f) / dir.y;   // y = 1

                List<float> hits = new();
                if (tLeft   > 0) hits.Add(tLeft);
                if (tRight  > 0) hits.Add(tRight);
                if (tBottom > 0) hits.Add(tBottom);
                if (tTop    > 0) hits.Add(tTop);

                if (hits.Count == 0)
                    continue;

                float tEdge = hits[0];
                for (int i = 1; i < hits.Count; i++)
                    if (hits[i] < tEdge)
                        tEdge = hits[i];

                // Edge intersection in viewport coords
                Vector2 vpEdge = new Vector2(
                    0.5f + dir.x * tEdge,
                    0.5f + dir.y * tEdge
                );

                // Clamp to edges
                vpEdge.x = Mathf.Clamp(vpEdge.x, 0f, 1f);
                vpEdge.y = Mathf.Clamp(vpEdge.y, 0f, 1f);

                // ------------------------------------------------------
                // Detect which edge we hit & push indicator inward
                // ------------------------------------------------------

                // Convert pixel offsets to viewport scale
                float offsetX = edgeOffsetHorizontal / Screen.width;
                float offsetY = edgeOffsetVertical / Screen.height;

                bool hitLeft   = Mathf.Approximately(vpEdge.x, 0f);
                bool hitRight  = Mathf.Approximately(vpEdge.x, 1f);
                bool hitBottom = Mathf.Approximately(vpEdge.y, 0f);
                bool hitTop    = Mathf.Approximately(vpEdge.y, 1f);

                if (hitLeft)   vpEdge.x += offsetX;
                if (hitRight)  vpEdge.x -= offsetX;
                if (hitBottom) vpEdge.y += offsetY;
                if (hitTop)    vpEdge.y -= offsetY;

                // Soft clamp
                vpEdge.x = Mathf.Clamp(vpEdge.x, clampPadding, 1f - clampPadding);
                vpEdge.y = Mathf.Clamp(vpEdge.y, clampPadding, 1f - clampPadding);

                // ------------------------------------------------------
                // Convert to screen and canvas space
                // ------------------------------------------------------
                Vector3 screenPoint = targetCamera.ViewportToScreenPoint(
                    new Vector3(vpEdge.x, vpEdge.y, vp.z)
                );

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPoint,
                    uiCamera,
                    out Vector2 localPos
                );

                // Arrow rotation
                float angleDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                // Request indicator from pool
                OffscreenIndicator arrow = indicatorPool.Get();
                arrow.SetPositionAndRotation(localPos, angleDeg);

                activeIndicators.Add(arrow);
            }
        }
    }
}
