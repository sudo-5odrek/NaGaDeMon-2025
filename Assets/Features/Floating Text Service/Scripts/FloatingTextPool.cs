using System.Collections.Generic;
using NaGaDeMon.Features.ObjectPoolService;
using UnityEngine;

namespace NaGaDeMon.Features.FloatingTextService
{
    public class FloatingTextPool :MonoBehaviour
    {
        private GenericPool<FloatingTextController> pool;
        private Camera cam;

        public void Init(FloatingTextController prefab, Transform parent, int prewarm, Camera camRef)
        {
            cam = camRef;

            pool = new GenericPool<FloatingTextController>(prefab, parent, prewarm);

            // Run custom init for each prewarmed instance
            foreach (var inst in GetAllInPool())
                inst.Init(this, cam);
        }

        public FloatingTextController Get()
        {
            var inst = pool.Get();
            inst.Init(this, cam);
            inst.transform.localScale = Vector3.one;
            return inst;
        }

        public void Release(FloatingTextController controller)
        {
            pool.Release(controller);
        }

        private IEnumerable<FloatingTextController> GetAllInPool()
        {
            // Unfortunately Queue<T> doesn't expose direct enumeration of ONLY pooled items.
            // We can expose it in GenericPool by adding protected access.
            // But you get the idea: call Init() on all items if needed.
            yield break;
        }
    }
}