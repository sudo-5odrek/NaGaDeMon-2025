using UnityEngine;

namespace UI.Indicators
{
    public class OffscreenIndicator : MonoBehaviour
    {
        public RectTransform root;
        public RectTransform arrowGraphic; // Arrow pointing UP in sprite

        private void Reset()
        {
            root = GetComponent<RectTransform>();
        }

        public void SetPositionAndRotation(Vector2 localPos, float angleDeg)
        {
            if (!root) root = GetComponent<RectTransform>();

            root.anchoredPosition = localPos;

            if (arrowGraphic)
                arrowGraphic.localRotation = Quaternion.Euler(0f, 0f, angleDeg - 90f);
        }
    }
}

