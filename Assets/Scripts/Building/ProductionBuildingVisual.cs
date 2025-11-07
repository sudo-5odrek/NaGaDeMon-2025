using UnityEngine;

[RequireComponent(typeof(ProductionBuilding))]
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

    private ProductionBuilding prod;
    private float maskHalfHeight;

    private void Awake()
    {
        prod = GetComponent<ProductionBuilding>();

        if (orangeRenderer) orangeRenderer.color = orangeColor;
        if (blackRenderer) blackRenderer.color = blackColor;

        // Calculate half-height of mask sprite
        if (spriteMask && spriteMask.sprite != null)
        {
            maskHalfHeight = spriteMask.sprite.bounds.size.y * maskTransform.localScale.y * 0.5f;
        }
    }

    private void Update()
    {
        if (prod == null || prod.ActiveRecipe == null)
            return;

        // Compute normalized progress directly from craft timer
        float progress = 0f;
        if (prod.IsCrafting)
        {
            float t = Mathf.Clamp01(1f - (prod.CraftTimer / prod.ActiveRecipe.craftTime));
            progress = t;
        }

        // Move mask bottom→top over the exact craft duration
        if (maskTransform)
        {
            Vector3 pos = maskTransform.localPosition;
            pos.y = Mathf.Lerp(-maskHalfHeight, maskHalfHeight, progress);
            maskTransform.localPosition = pos;
        }

        // Animate optional moving part
        if (animatedPart)
        {
            //animatedPart.gameObject.SetActive(prod.IsCrafting);

            if (prod.IsCrafting)
                animatedPart.Rotate(0, 0, 180f * Time.deltaTime);
        }
    }
}
