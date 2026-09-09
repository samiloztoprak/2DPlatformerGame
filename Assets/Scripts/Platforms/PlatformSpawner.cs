using Game.Data;
using UnityEngine;

namespace Game.Platforms
{
    public class PlatformSpawner
    {
        private readonly PlatformSpawnSetSO _spawnSet;
        private readonly PlatformPoolManager _poolManager;
        private float _nextSpawnY;

        public PlatformSpawner(PlatformSpawnSetSO spawnSet, PlatformPoolManager poolManager, float startY)
        {
            _spawnSet = spawnSet;
            _poolManager = poolManager;
            _nextSpawnY = startY;
        }

        public bool ShouldSpawnMore(float targetHeight)
        {
            return _nextSpawnY < targetHeight;
        }

        public PlatformController SpawnNext(float minX, float maxX)
        {
            return SpawnAt(Random.Range(minX, maxX));
        }

        public PlatformController SpawnAt(float x)
        {
            var data = _spawnSet.GetRandomPlatform();
            var platform = _poolManager.Get(data);

            platform.PlaceAt(new Vector3(x, _nextSpawnY, 0f));

            _nextSpawnY += Random.Range(data.MinVerticalGap, data.MaxVerticalGap);

            return platform;
        }
    }
}
