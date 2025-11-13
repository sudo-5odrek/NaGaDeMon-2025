using System.Collections.Generic;
using UnityEngine;

namespace Floating_Text_Service
{
    public class FloatingTextPool : MonoBehaviour
    {
        [SerializeField] private FloatingTextController prefab;
        [SerializeField] private Transform parent; // world-space canvas transform
        [SerializeField] private int prewarmCount = 16;
        private readonly Queue<FloatingTextController> pool = new();

        private Camera mainCam;

        public void Init(FloatingTextController prefabRef, Transform parentRef, int prewarm, Camera cam)
        {
            prefab = prefabRef;
            parent = parentRef;
            prewarmCount = Mathf.Max(0, prewarm);
            mainCam = cam;

            for (int i = 0; i < prewarmCount; i++)
                pool.Enqueue(CreateInstance());
        }

        private FloatingTextController CreateInstance()
        {
            var inst = Instantiate(prefab, parent);
            inst.gameObject.SetActive(false);
            inst.Init(this, mainCam);
            return inst;
        }

        public FloatingTextController Get()
        {
            if (pool.Count == 0)
                pool.Enqueue(CreateInstance());

            var inst = pool.Dequeue();
            inst.gameObject.SetActive(true);
            inst.transform.localScale = Vector3.one; // reset potential scale changes
            return inst;
        }

        public void Release(FloatingTextController controller)
        {
            controller.gameObject.SetActive(false);
            pool.Enqueue(controller);
        }
    }
}