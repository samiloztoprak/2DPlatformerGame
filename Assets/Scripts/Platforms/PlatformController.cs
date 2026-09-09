using Game.Core.Pooling;
using Game.Data;
using UnityEngine;

namespace Game.Platforms
{
    [RequireComponent(typeof(Collider2D))]
    public class PlatformController : MonoBehaviour, IPoolable
    {
        private IPlatformMovement _movement;
        private Vector3 _basePosition;
        private float _elapsedTime;

        private SpriteRenderer _spriteRenderer;
        private Collider2D _collider;
        private bool _isCrumbling;
        private float _crumbleDelayRemaining;
        private float _crumbleFallSpeed;

        public PlatformDataSO Data { get; private set; }

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<Collider2D>();
        }

        public void Initialize(PlatformDataSO data)
        {
            Data = data;
            _movement = PlatformMovementFactory.Create(data.MovementType);

            if (data.Sprite != null)
            {
                _spriteRenderer.sprite = data.Sprite;
            }
        }

        public void PlaceAt(Vector3 position)
        {
            transform.position = position;
            _basePosition = position;
            _elapsedTime = 0f;
        }

        public void NotifyLanded()
        {
            if (Data.IsCrumbling && !_isCrumbling)
            {
                _isCrumbling = true;
                _crumbleDelayRemaining = Data.CrumbleDelay;
            }
        }

        public void OnSpawned()
        {
        }

        public void OnDespawned()
        {
            Data = null;
            _movement = null;
            _isCrumbling = false;
            _crumbleDelayRemaining = 0f;
            _crumbleFallSpeed = 0f;
            _collider.enabled = true;
        }

        private void Update()
        {
            if (_isCrumbling)
            {
                UpdateCrumble();
                return;
            }

            if (_movement == null)
            {
                return;
            }

            _elapsedTime += Time.deltaTime;
            transform.position = _movement.Evaluate(_basePosition, _elapsedTime, Data);
        }

        private void UpdateCrumble()
        {
            if (_crumbleDelayRemaining > 0f)
            {
                _crumbleDelayRemaining -= Time.deltaTime;
                return;
            }

            _collider.enabled = false;
            _crumbleFallSpeed += Data.CrumbleFallAcceleration * Time.deltaTime;
            transform.position += Vector3.down * (_crumbleFallSpeed * Time.deltaTime);
        }
    }
}
