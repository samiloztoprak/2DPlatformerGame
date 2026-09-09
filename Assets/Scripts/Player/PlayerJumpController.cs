using Game.Data;
using Game.Platforms;
using UnityEngine;

namespace Game.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerJumpController : MonoBehaviour
    {
        [SerializeField] private GameConfigSO _config;
        [SerializeField] private PlatformEventChannelSO _playerLandedChannel;

        private Rigidbody2D _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // The platform's one-way effector already guarantees this collision only
            // happens when landing from above, so no extra velocity check is needed here.
            var platform = collision.collider.GetComponentInParent<PlatformController>();
            if (platform == null)
            {
                return;
            }

            _rigidbody.linearVelocity = new Vector2(_rigidbody.linearVelocity.x, _config.JumpForce);
            platform.NotifyLanded();
            _playerLandedChannel.Raise(platform);
        }
    }
}
