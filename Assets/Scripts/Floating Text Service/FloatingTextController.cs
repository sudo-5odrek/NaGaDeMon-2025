using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Floating_Text_Service
{
    public class FloatingTextController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private TextMeshProUGUI tmp;
        [SerializeField] private Image iconImage;   // ← assign in inspector

        private FloatingTextPool originPool;
        private Camera mainCam;
        private Coroutine animRoutine;

        private FloatingTextStyle style;
        private Color baseColor;

        // Called once by pool when created
        public void Init(FloatingTextPool pool, Camera cam)
        {
            originPool = pool;
            mainCam = cam;
        }

        /// <summary>
        /// Begin displaying the floating text instance using style + data overrides.
        /// </summary>
        public void Play(FloatingTextStyle style, FloatingTextData data)
        {
            this.style = style;

            // ----------------------------
            // TEXT
            // ----------------------------
            tmp.text = data.text ?? "?";

            if (style.font) 
                tmp.font = style.font;

            tmp.fontSize = (data.fontSizeOverride > 0f) 
                ? data.fontSizeOverride 
                : style.fontSize;

            // ----------------------------
            // COLOR
            // ----------------------------
            baseColor = data.color.HasValue ? data.color.Value : style.color;
            tmp.color = baseColor;

            // Outline
            var mat = tmp.fontMaterial;
            if (style.useOutline)
            {
                mat.EnableKeyword("OUTLINE_ON");
                mat.SetColor(ShaderUtilities.ID_OutlineColor, style.outlineColor);
                mat.SetFloat(ShaderUtilities.ID_OutlineWidth, style.outlineWidth);
            }
            else
            {
                mat.DisableKeyword("OUTLINE_ON");
                mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
            }

            // ----------------------------
            // ICON SELECTION
            // ----------------------------
            Sprite iconToUse = data.iconOverride != null ? data.iconOverride : style.icon;

            if (iconToUse == null)
            {
                iconImage.gameObject.SetActive(false);
            }
            else
            {
                iconImage.gameObject.SetActive(true);
                iconImage.sprite = iconToUse;

                Vector2 sizeToUse = data.iconSizeOverride.HasValue
                    ? data.iconSizeOverride.Value
                    : style.iconSize;

                iconImage.rectTransform.sizeDelta = sizeToUse;
            }

            // ----------------------------
            // POSITIONING
            // ----------------------------
            transform.position = data.worldPosition + style.spawnOffset;

            // Reset scale
            rectTransform.localScale = Vector3.one;

            // ----------------------------
            // ANIMATION
            // ----------------------------
            if (animRoutine != null)
                StopCoroutine(animRoutine);

            animRoutine = StartCoroutine(Animate(data));
        }

        private IEnumerator Animate(FloatingTextData data)
        {
            float duration = (data.durationOverride > 0f) 
                ? data.durationOverride 
                : style.duration;

            Vector3 start = transform.position;
            Vector3 end = start + style.endOffset;

            float t = 0f;

            while (t < duration)
            {
                float p = t / duration;

                // ↑ MOTION
                float m = style.motionCurve.Evaluate(p);
                transform.position = Vector3.LerpUnclamped(start, end, m);

                // 🟡 FADE
                float a = style.fadeCurve.Evaluate(p);
                tmp.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);

                // 🔍 SCALE (optional)
                if (style.useScaleCurve)
                {
                    float s = style.scaleCurve.Evaluate(p);
                    rectTransform.localScale = Vector3.one * s;
                }

                t += Time.deltaTime;
                yield return null;
            }

            // Ensure fully transparent
            tmp.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);

            // Return to pool
            originPool.Release(this);
        }
    }
}
