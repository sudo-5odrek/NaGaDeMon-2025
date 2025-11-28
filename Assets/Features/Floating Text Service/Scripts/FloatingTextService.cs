using UnityEngine;

namespace NaGaDeMon.Features.FloatingTextService
{
    public class FloatingTextService : MonoBehaviour
    {
        public static FloatingTextService Instance { get; private set; }

        [Header("Scene References")]
        [Tooltip("World Space canvas that hosts all floating texts")]
        public Canvas worldSpaceCanvas;

        [Tooltip("Pooled prefab with FloatingTextController + TMPUGUI")]
        public FloatingTextController pooledPrefab;

        [Min(0)] public int prewarmCount = 16;

        private FloatingTextPool pool;
        private Camera mainCam;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            mainCam = Camera.main;

            pool = gameObject.AddComponent<FloatingTextPool>();
            pool.Init(pooledPrefab, worldSpaceCanvas.transform, prewarmCount, mainCam);
        }

        /// <summary>
        /// Minimal call: show text with a style at a world position.
        /// </summary>
        public void Spawn(FloatingTextStyle style, Vector3 worldPosition, string text)
        {
            var data = FloatingTextData.FromWorld(text, worldPosition);
            Spawn(style, data);
        }

        /// <summary>
        /// Full control via overrides.
        /// </summary>
        public void Spawn(FloatingTextStyle style, FloatingTextData data)
        {
            var inst = pool.Get();
            inst.Play(style, data);
        }

        /// <summary>
        /// Convenience overload for numbers.
        /// </summary>
        public void Spawn(FloatingTextStyle style, Vector3 worldPosition, int value)
        {
            Spawn(style, worldPosition, value.ToString());
        }

        /// <summary>
        /// Convenience overload with quick overrides.
        /// </summary>
        public void SpawnQuick(FloatingTextStyle style, Vector3 worldPosition, string text,
            Color? overrideColor = null, float durationOverride = -1f, float fontSizeOverride = -1f)
        {
            var data = new FloatingTextData
            {
                text = text,
                worldPosition = worldPosition,
                color = overrideColor,
                durationOverride = durationOverride,
                fontSizeOverride = fontSizeOverride
            };
            Spawn(style, data);
        }
    }
}
