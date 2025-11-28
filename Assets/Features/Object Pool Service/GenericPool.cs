using System.Collections.Generic;
using UnityEngine;

namespace NaGaDeMon.Features.ObjectPoolService
{
    public class GenericPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform parent;

        private readonly Queue<T> pool = new();         // inactive objects
        private readonly HashSet<T> activeObjects = new(); // currently active objects

        public int Count => pool.Count;

        public IEnumerable<T> ActiveObjects => activeObjects;

        public GenericPool(T prefab, Transform parent, int prewarmCount = 0)
        {
            this.prefab = prefab;
            this.parent = parent;

            for (int i = 0; i < prewarmCount; i++)
                pool.Enqueue(CreateInstance());
        }

        private T CreateInstance()
        {
            var inst = Object.Instantiate(prefab, parent);
            inst.gameObject.SetActive(false);
            return inst;
        }

        public T Get()
        {
            if (pool.Count == 0)
                pool.Enqueue(CreateInstance());

            var inst = pool.Dequeue();
            inst.gameObject.SetActive(true);
            activeObjects.Add(inst);
            return inst;
        }

        public void Release(T instance)
        {
            if (activeObjects.Contains(instance))
                activeObjects.Remove(instance);

            instance.gameObject.SetActive(false);
            pool.Enqueue(instance);
        }

        /// <summary>
        /// Returns all pooled *inactive* objects (those still in the queue).
        /// </summary>
        public IEnumerable<T> GetAllPooledObjects()
        {
            return pool;
        }
    }
}