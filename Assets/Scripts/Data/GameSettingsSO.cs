using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Game Settings", fileName = "GameSettings")]
    public class GameSettingsSO : ScriptableObject
    {
        private const string MasterVolumeKey = "Settings.MasterVolume";
        private const string SfxVolumeKey = "Settings.SfxVolume";

        public float MasterVolume { get; private set; }
        public float SfxVolume { get; private set; }

        private void OnEnable()
        {
            MasterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
            SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        }

        public void SetMasterVolume(float linearVolume)
        {
            MasterVolume = linearVolume;
            PlayerPrefs.SetFloat(MasterVolumeKey, linearVolume);
        }

        public void SetSfxVolume(float linearVolume)
        {
            SfxVolume = linearVolume;
            PlayerPrefs.SetFloat(SfxVolumeKey, linearVolume);
        }
    }
}
