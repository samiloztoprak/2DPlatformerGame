using Game.Core.Events;
using UnityEngine;

namespace Game.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class SfxPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClipEventChannelSO _sfxRequestChannel;

        private AudioSource _audioSource;

        public AudioSource AudioSource => _audioSource;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            _sfxRequestChannel.OnEventRaised += HandleSfxRequested;
        }

        private void OnDisable()
        {
            _sfxRequestChannel.OnEventRaised -= HandleSfxRequested;
        }

        private void HandleSfxRequested(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            _audioSource.PlayOneShot(clip);
        }
    }
}
