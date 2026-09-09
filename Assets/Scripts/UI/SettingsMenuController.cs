using Game.Audio;
using Game.Core.Events;
using Game.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class SettingsMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject _mainMenu;
        [SerializeField] private GameSettingsSO _gameSettings;
        [SerializeField] private AudioSettingsController _audioSettings;
        [SerializeField] private AudioClipEventChannelSO _sfxRequestChannel;
        [SerializeField] private AudioClip _clickClip;
        [SerializeField] private AudioClip _switchClip;

        private UIDocument _document;
        private Slider _masterVolumeSlider;
        private Slider _sfxVolumeSlider;
        private Button _backButton;
        private bool _mainMenuWasActive;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            var root = _document.rootVisualElement;

            _masterVolumeSlider = root.Q<Slider>("MasterVolumeSlider");
            _sfxVolumeSlider = root.Q<Slider>("SfxVolumeSlider");
            _backButton = root.Q<Button>("BackButton");

            _masterVolumeSlider.SetValueWithoutNotify(_gameSettings.MasterVolume);
            _sfxVolumeSlider.SetValueWithoutNotify(_gameSettings.SfxVolume);

            _masterVolumeSlider.RegisterValueChangedCallback(HandleMasterVolumeChanged);
            _sfxVolumeSlider.RegisterValueChangedCallback(HandleSfxVolumeChanged);
            _backButton.clicked += HandleBackClicked;
        }

        private void OnDisable()
        {
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.UnregisterValueChangedCallback(HandleMasterVolumeChanged);
            }

            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.UnregisterValueChangedCallback(HandleSfxVolumeChanged);
            }

            if (_backButton != null)
            {
                _backButton.clicked -= HandleBackClicked;
            }
        }

        public void Open()
        {
            _mainMenuWasActive = _mainMenu.activeSelf;
            if (_mainMenuWasActive)
            {
                _mainMenu.SetActive(false);
            }

            gameObject.SetActive(true);
            Time.timeScale = 0f;
        }

        private void HandleMasterVolumeChanged(ChangeEvent<float> evt)
        {
            _audioSettings.SetMasterVolume(evt.newValue);
        }

        private void HandleSfxVolumeChanged(ChangeEvent<float> evt)
        {
            _audioSettings.SetSfxVolume(evt.newValue);
            _sfxRequestChannel.Raise(_switchClip);
        }

        private void HandleBackClicked()
        {
            _sfxRequestChannel.Raise(_clickClip);
            Time.timeScale = 1f;
            gameObject.SetActive(false);

            if (_mainMenuWasActive)
            {
                _mainMenu.SetActive(true);
            }
        }
    }
}
