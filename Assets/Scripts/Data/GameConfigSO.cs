using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Game Config", fileName = "GameConfig")]
    public class GameConfigSO : ScriptableObject
    {
        [Header("Player")]
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private float _playerMoveSpeed = 6f;
        [SerializeField] private float _jumpForce = 14f;
        [SerializeField] private float _screenWrapMargin = 0.3f;
        [SerializeField] private float _fallDeathMargin = 1f;

        [Header("Camera")]
        [SerializeField] private float _cameraFollowSmoothTime = 0.25f;
        [SerializeField] private float _maxCameraLag = 3f;

        [Header("Platforms")]
        [SerializeField] private int _initialPlatformCount = 12;
        [SerializeField] private float _spawnAheadDistance = 10f;
        [SerializeField] private float _despawnBehindDistance = 2f;

        public GameObject PlayerPrefab => _playerPrefab;
        public float PlayerMoveSpeed => _playerMoveSpeed;
        public float JumpForce => _jumpForce;
        public float ScreenWrapMargin => _screenWrapMargin;
        public float FallDeathMargin => _fallDeathMargin;
        public float CameraFollowSmoothTime => _cameraFollowSmoothTime;
        public float MaxCameraLag => _maxCameraLag;
        public int InitialPlatformCount => _initialPlatformCount;
        public float SpawnAheadDistance => _spawnAheadDistance;
        public float DespawnBehindDistance => _despawnBehindDistance;
    }
}
