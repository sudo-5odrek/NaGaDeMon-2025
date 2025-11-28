using UnityEngine;

namespace Features.Building.Scripts.Production
{
    public class ProductionBuildingVisualMask : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform maskTransform;
        [SerializeField] private SpriteMask spriteMask;
        [SerializeField] private SpriteRenderer orangeRenderer;
        [SerializeField] private SpriteRenderer blackRenderer;
        [SerializeField] private Transform animatedPart;

        [Header("Colors")]
        [SerializeField] private Color orangeColor = new Color(1f, 0.5f, 0f);
        [SerializeField] private Color blackColor = Color.black;

        private IHasProgress buildingProgress;
        private float maskHalfHeight;

        private void Awake()
        {
            buildingProgress = GetComponent<IHasProgress>();
            if (buildingProgress == null)
            {
                Debug.LogError($"{name}: Missing component implementing IHasProgress");
                enabled = false;
                return;
            }

            if (orangeRenderer) orangeRenderer.color = orangeColor;
            if (blackRenderer) blackRenderer.color = blackColor;

            if (spriteMask && spriteMask.sprite != null)
                maskHalfHeight = spriteMask.sprite.bounds.size.y * maskTransform.localScale.y * 0.5f;
        }

        private void Update()
        {
            float progress = buildingProgress.ProgressNormalized;

            if (maskTransform)
            {
                Vector3 pos = maskTransform.localPosition;
                pos.y = Mathf.Lerp(-maskHalfHeight, maskHalfHeight, progress);
                maskTransform.localPosition = pos;
            }

            if (animatedPart && buildingProgress.IsProcessing)
                animatedPart.Rotate(0, 0, 180f * Time.deltaTime);
        }
    }

    // Place interface OUTSIDE the class
    public interface IHasProgress
    {
        bool IsProcessing { get; }
        float ProgressNormalized { get; }
    }
}