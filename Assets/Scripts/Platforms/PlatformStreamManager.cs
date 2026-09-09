using System.Collections.Generic;
using Game.Core.Utility;
using Game.Data;
using UnityEngine;

namespace Game.Platforms
{
    public class PlatformStreamManager : MonoBehaviour
    {
        [SerializeField] private GameConfigSO _config;
        [SerializeField] private PlatformSpawnSetSO _spawnSet;
        [SerializeField] private Transform _platformParent;

        private readonly List<PlatformController> _activePlatforms = new();

        private PlatformPoolManager _poolManager;
        private PlatformSpawner _spawner;
        private PlatformRecycler _recycler;
        private Transform _player;
        private Camera _trackingCamera;

        public void Initialize(Transform player, Camera trackingCamera)
        {
            _player = player;
            _trackingCamera = trackingCamera;

            _poolManager = new PlatformPoolManager(_spawnSet, _platformParent);
            _spawner = new PlatformSpawner(_spawnSet, _poolManager, startY: player.position.y - 1f);
            _recycler = new PlatformRecycler(_poolManager);

            GenerateInitialPlatforms();
        }

        private void Update()
        {
            if (_player == null)
            {
                return;
            }

            float targetHeight = _player.position.y + _config.SpawnAheadDistance;
            while (_spawner.ShouldSpawnMore(targetHeight))
            {
                SpawnOne();
            }

            float recycleThreshold = _trackingCamera.transform.position.y - _config.DespawnBehindDistance - _trackingCamera.orthographicSize;
            _recycler.RecycleBelow(_activePlatforms, recycleThreshold);
        }

        private void GenerateInitialPlatforms()
        {
            var firstPlatform = _spawner.SpawnAt(_player.position.x);
            _activePlatforms.Add(firstPlatform);

            for (int i = 1; i < _config.InitialPlatformCount; i++)
            {
                SpawnOne();
            }
        }

        private void SpawnOne()
        {
            var bounds = ScreenBoundsUtility.GetWorldBounds(_trackingCamera);
            var platform = _spawner.SpawnNext(bounds.xMin, bounds.xMax);
            _activePlatforms.Add(platform);
        }
    }
}
