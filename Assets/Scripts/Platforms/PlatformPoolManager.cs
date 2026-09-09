using System.Collections.Generic;
using Game.Core.Pooling;
using Game.Data;
using UnityEngine;

namespace Game.Platforms
{
    public class PlatformPoolManager
    {
        private readonly Dictionary<PlatformDataSO, ComponentPool<PlatformController>> _pools = new();

        public PlatformPoolManager(PlatformSpawnSetSO spawnSet, Transform parent)
        {
            foreach (var entry in spawnSet.Entries)
            {
                var prefab = entry.Data.Prefab.GetComponent<PlatformController>();
                _pools[entry.Data] = new ComponentPool<PlatformController>(prefab, parent);
            }
        }

        public PlatformController Get(PlatformDataSO data)
        {
            var platform = _pools[data].Get();
            platform.Initialize(data);
            return platform;
        }

        public void Release(PlatformController platform)
        {
            _pools[platform.Data].Release(platform);
        }
    }
}
