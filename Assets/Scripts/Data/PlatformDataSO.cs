using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Platform Data", fileName = "PlatformData")]
    public class PlatformDataSO : ScriptableObject
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private float _minVerticalGap = 1.5f;
        [SerializeField] private float _maxVerticalGap = 2.5f;
        [SerializeField] private PlatformMovementType _movementType;
        [SerializeField] private float _moveSpeed = 1f;
        [SerializeField] private float _moveRange = 2f;

        [Header("Crumbling")]
        [SerializeField] private bool _isCrumbling;
        [SerializeField] private float _crumbleDelay = 0.4f;
        [SerializeField] private float _crumbleFallAcceleration = 20f;

        public GameObject Prefab => _prefab;
        public Sprite Sprite => _sprite;
        public float MinVerticalGap => _minVerticalGap;
        public float MaxVerticalGap => _maxVerticalGap;
        public PlatformMovementType MovementType => _movementType;
        public float MoveSpeed => _moveSpeed;
        public float MoveRange => _moveRange;
        public bool IsCrumbling => _isCrumbling;
        public float CrumbleDelay => _crumbleDelay;
        public float CrumbleFallAcceleration => _crumbleFallAcceleration;
    }
}
