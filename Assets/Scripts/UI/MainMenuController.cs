using Game.Bootstrap;
using Game.Core.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameInitializer _gameInitializer;
        [SerializeField] private Sprite _playButtonNormalSprite;
        [SerializeField] private Sprite _playButtonPressedSprite;
        [SerializeField] private RectOffset _playButtonSlice;
        [SerializeField] private AudioClipEventChannelSO _sfxRequestChannel;
        [SerializeField] private AudioClip _clickClip;

        private UIDocument _document;
        private Button _playButton;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            var root = _document.rootVisualElement;

            _playButton = root.Q<Button>("PlayButton");
            _playButton.clicked += HandlePlayClicked;
            new KenneyButtonSkinner(_playButton, _playButtonNormalSprite, _playButtonPressedSprite, _playButtonSlice);
        }

        private void OnDisable()
        {
            if (_playButton != null)
            {
                _playButton.clicked -= HandlePlayClicked;
            }
        }

        private void HandlePlayClicked()
        {
            _sfxRequestChannel.Raise(_clickClip);
            gameObject.SetActive(false);
            _gameInitializer.BeginGame();
        }
    }
}
