using Game.Data;
using UnityEngine;

namespace Game.Audio
{
    public class AudioSettingsController : MonoBehaviour
    {
        [SerializeField] private GameSettingsSO _gameSettings;
        [SerializeField] private SfxPlayer _sfxPlayer;

        private void Start()
        {
            ApplyVolume();
        }

        public void SetMasterVolume(float linearVolume)
        {
            _gameSettings.SetMasterVolume(linearVolume);
            ApplyVolume();
        }

        public void SetSfxVolume(float linearVolume)
        {
            _gameSettings.SetSfxVolume(linearVolume);
            ApplyVolume();
        }

        private void ApplyVolume()
        {
            _sfxPlayer.AudioSource.volume = _gameSettings.MasterVolume * _gameSettings.SfxVolume;
        }
    }
}
