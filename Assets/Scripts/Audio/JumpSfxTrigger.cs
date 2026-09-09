using Game.Core.Events;
using Game.Platforms;
using UnityEngine;

namespace Game.Audio
{
    public class JumpSfxTrigger : MonoBehaviour
    {
        [SerializeField] private PlatformEventChannelSO _playerLandedChannel;
        [SerializeField] private AudioClipEventChannelSO _sfxRequestChannel;
        [SerializeField] private AudioClip _jumpClip;

        private void OnEnable()
        {
            _playerLandedChannel.OnEventRaised += HandlePlayerLanded;
        }

        private void OnDisable()
        {
            _playerLandedChannel.OnEventRaised -= HandlePlayerLanded;
        }

        private void HandlePlayerLanded(PlatformController platform)
        {
            _sfxRequestChannel.Raise(_jumpClip);
        }
    }
}
