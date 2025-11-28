using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Inventory.Scripts
{
    public class ItemWheelUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform wheelRoot;
        [SerializeField] private Image segmentTemplate;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Settings")]
        [SerializeField] private float radius = 120f;

        private List<ItemDefinition> items;
        private readonly List<Image> segments = new();

        private float hoverSelectTime;
        private float hoverTimer;

        private Action<ItemDefinition> onSelected;
        [HideInInspector] public int hoveredIndex = -1;

        private Camera cam;

        private void Awake()
        {
            cam = Camera.main;
            canvas.worldCamera = cam;

            // Ensure template is disabled at start
            segmentTemplate.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------
        // PUBLIC API – CALLED BY PlayerInteraction
        // ---------------------------------------------------------
        public void Open(
            Vector2 screenPos,
            List<ItemDefinition> itemDefs,
            float hoverTime,
            Action<ItemDefinition> callback)
        {
            // Save refs
            items = itemDefs;
            hoverSelectTime = hoverTime;
            onSelected = callback;

            // FULL UI RESET
            hoveredIndex = -1;
            hoverTimer = 0f;

            foreach (var seg in segments)
                Destroy(seg.gameObject);
            segments.Clear();

            // Convert screen → canvas local
            RectTransform canvasRect = canvas.transform as RectTransform;
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                canvas.worldCamera,
                out localPos
            );

            wheelRoot.anchoredPosition = localPos;

            BuildSegments();
        }

        // ---------------------------------------------------------
        // BUILD UI SEGMENTS
        // ---------------------------------------------------------
        private void BuildSegments()
        {
            float angleStep = 360f / items.Count;

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];

                // Clone segment
                Image seg = Instantiate(segmentTemplate, wheelRoot);
                seg.gameObject.SetActive(true);     // activate clone
                segments.Add(seg);

                // Place in circle
                float angle = angleStep * i - 90f;
                Vector2 pos = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                );

                seg.rectTransform.anchoredPosition = pos * radius;
                seg.rectTransform.localRotation = Quaternion.identity;

                // Assign icon
                seg.sprite = item.icon;
                seg.color = new Color(1f, 1f, 1f, 0.55f);

                // Add hover script
                var segScript = seg.gameObject.AddComponent<ItemWheelSegment>();
                segScript.Initialize(this, i);
            }
        }

        // ---------------------------------------------------------
        // CALLED BY ItemWheelSegment WHEN HOVERED
        // ---------------------------------------------------------
        public void OnSegmentHovered(int index)
        {
            hoveredIndex = index;
            hoverTimer = 0f;

            // Reset all
            for (int i = 0; i < segments.Count; i++)
                segments[i].color = new Color(1f, 1f, 1f, 0.55f);

            // Highlight hovered
            segments[index].color = Color.white;
        }

        // ---------------------------------------------------------
        // TIMER + CALLBACK
        // ---------------------------------------------------------
        private void Update()
        {
            if (hoveredIndex == -1)
                return;

            hoverTimer += Time.deltaTime;

            if (hoverTimer >= hoverSelectTime)
            {
                onSelected?.Invoke(items[hoveredIndex]);
                hoveredIndex = -1;
                hoverTimer = 0f;

                // Close wheel (but do not destroy it)
                gameObject.SetActive(false);
            }
        }
    }
}
