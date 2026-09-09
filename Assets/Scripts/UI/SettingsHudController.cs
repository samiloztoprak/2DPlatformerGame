using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class SettingsHudController : MonoBehaviour
    {
        [SerializeField] private SettingsMenuController _settingsMenu;
        [SerializeField] private Sprite _buttonNormalSprite;
        [SerializeField] private Sprite _buttonPressedSprite;
        [SerializeField] private Sprite _iconSprite;

        private UIDocument _document;
        private Button _settingsButton;
        private VisualElement _icon;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            var root = _document.rootVisualElement;

            _settingsButton = root.Q<Button>("SettingsButton");
            _icon = root.Q<VisualElement>("SettingsIcon");
            _icon.style.backgroundImage = new StyleBackground(_iconSprite);

            new KenneyButtonSkinner(_settingsButton, _buttonNormalSprite, _buttonPressedSprite, new RectOffset(0, 0, 0, 0));

            _settingsButton.clicked += HandleSettingsClicked;
        }

        private void OnDisable()
        {
            if (_settingsButton != null)
            {
                _settingsButton.clicked -= HandleSettingsClicked;
            }
        }

        private void HandleSettingsClicked()
        {
            _settingsMenu.Open();
        }
    }
}
