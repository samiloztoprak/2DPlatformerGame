using UnityEngine;
using UnityEngine.Pool;

namespace Game.Core.Pooling
{
    public class ComponentPool<T> where T : Component, IPoolable
    {
        private readonly ObjectPool<T> _pool;
        private readonly T _prefab;
        private readonly Transform _parent;

        public ComponentPool(T prefab, Transform parent, int defaultCapacity = 8, int maxSize = 64)
        {
            _prefab = prefab;
            _parent = parent;
            _pool = new ObjectPool<T>(CreateInstance, OnGet, OnRelease, OnDestroyItem, true, defaultCapacity, maxSize);
        }

        public T Get()
        {
            return _pool.Get();
        }

        public void Release(T item)
        {
            _pool.Release(item);
        }

        private T CreateInstance()
        {
            var instance = Object.Instantiate(_prefab, _parent);
            instance.gameObject.SetActive(false);
            return instance;
        }

        private void OnGet(T item)
        {
            item.gameObject.SetActive(true);
            item.OnSpawned();
        }

        private void OnRelease(T item)
        {
            item.OnDespawned();
            item.gameObject.SetActive(false);
        }

        private void OnDestroyItem(T item)
        {
            if (item != null)
            {
                Object.Destroy(item.gameObject);
            }
        }
    }
}
