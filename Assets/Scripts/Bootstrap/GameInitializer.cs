using Game.CameraSystem;
using Game.Data;
using Game.Platforms;
using UnityEngine;

namespace Game.Bootstrap
{
    public class GameInitializer : MonoBehaviour
    {
        [SerializeField] private GameConfigSO _config;
        [SerializeField] private PlatformStreamManager _platformStreamManager;
        [SerializeField] private CameraFollowController _cameraFollow;
        [SerializeField] private Transform _playerSpawnPoint;

        public void BeginGame()
        {
            var player = Instantiate(_config.PlayerPrefab, _playerSpawnPoint.position, Quaternion.identity);

            _cameraFollow.SetTarget(player.transform);
            _platformStreamManager.Initialize(player.transform, Camera.main);
        }
    }
}
