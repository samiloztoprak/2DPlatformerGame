using Game.Core.Events;
using Game.Data;
using UnityEngine;

namespace Game.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovementController : MonoBehaviour
    {
        [SerializeField] private GameConfigSO _config;
        [SerializeField] private FloatEventChannelSO _moveDirectionChannel;

        private Rigidbody2D _rigidbody;
        private float _moveDirection;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            _moveDirectionChannel.OnEventRaised += HandleMoveDirectionChanged;
        }

        private void OnDisable()
        {
            _moveDirectionChannel.OnEventRaised -= HandleMoveDirectionChanged;
        }

        private void FixedUpdate()
        {
            _rigidbody.linearVelocity = new Vector2(_moveDirection * _config.PlayerMoveSpeed, _rigidbody.linearVelocity.y);
        }

        private void HandleMoveDirectionChanged(float direction)
        {
            _moveDirection = direction;
        }
    }
}
