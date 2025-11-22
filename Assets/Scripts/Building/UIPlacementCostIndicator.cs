using TMPro;
using UnityEngine;

namespace Building
{
    public class UIPlacementCostIndicator : MonoBehaviour
    {
        public static UIPlacementCostIndicator Instance;

        public RectTransform root;
        public TextMeshProUGUI costText;

        private Camera cam;

        void Awake()
        {
            Instance = this;
            cam = Camera.main;
            Hide();
        }

        public void ShowCost(BuildingData data, int count, Vector3 worldPos)
        {
            if (data == null) return;

            // Build the cost label
            string s = "";
            foreach (var c in data.cost)
            {
                int total = c.amount * count;
                s += $"{c.item.itemID}: {total}\n";
            }
            costText.text = s.TrimEnd();

            // Convert world → screen
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

            // Add small top-right offset
            screenPos.x += 0f;
            screenPos.y += 50f;

            // Get parent canvas RectTransform
            Canvas canvas = root.GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();

            // Convert screen → canvas local space
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam,
                out localPoint
            );

            // Assign anchored position
            root.anchoredPosition = localPoint;

            // Show UI
            root.gameObject.SetActive(true);
        }

        public void Hide()
        {
            root.gameObject.SetActive(false);
        }
        
        public void SetColor(Color c)
        {
            costText.color = c;
            // Optional: tint background too if you have one
        }
    }
}