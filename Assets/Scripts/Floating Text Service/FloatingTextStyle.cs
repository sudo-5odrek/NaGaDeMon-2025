using TMPro;
using UnityEngine;

namespace Floating_Text_Service
{
    [CreateAssetMenu(menuName = "UI/Floating Text Style", fileName = "FloatingTextStyle")]
    public class FloatingTextStyle : ScriptableObject
    {
        [Header("Visuals")]
        public TMP_FontAsset font;
        [Min(1)] public float fontSize = 36f;
        public Color color = Color.white;
        public bool useOutline = false;
        public Color outlineColor = Color.black;
        [Range(0f, 1f)] public float outlineWidth = 0.2f;
        public Sprite icon;
        public Vector2 iconSize = new Vector2(24, 24);

        [Header("Timing")]
        [Min(0.05f)] public float duration = 1.0f;

        [Header("Motion - Vertical")]
        public Vector3 spawnOffset = new Vector3(0f, 0.75f, 0f);
        public Vector3 endOffset = new Vector3(0f, 1.5f, 0f);
        public AnimationCurve motionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Motion - Horizontal (optional)")]
        public bool useHorizontalMotion = false;

        // 0 → 1 → 0 typical jiggle curve
        public AnimationCurve xMotionCurve = new AnimationCurve(
            new Keyframe(0f, 0.5f),
            new Keyframe(0.25f, 1f),
            new Keyframe(0.5f, 0.5f),
            new Keyframe(0.75f, 0f),
            new Keyframe(1f, 0.5f)
        );

        [Min(0f)] public float xAmplitude = 0.3f;

        [Header("Fade")]
        public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Header("Scale (optional)")]
        public bool useScaleCurve = false;
        public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1.1f, 1, 1f);
    }
}