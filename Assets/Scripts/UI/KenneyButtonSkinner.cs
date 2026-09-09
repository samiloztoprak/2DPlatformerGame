using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UI
{
    public class KenneyButtonSkinner
    {
        private readonly Button _button;
        private readonly Sprite _normalSprite;
        private readonly Sprite _pressedSprite;

        public KenneyButtonSkinner(Button button, Sprite normalSprite, Sprite pressedSprite, RectOffset slice)
        {
            _button = button;
            _normalSprite = normalSprite;
            _pressedSprite = pressedSprite;

            ApplySlice(slice);
            SetSprite(_normalSprite);

            _button.RegisterCallback<PointerDownEvent>(_ => SetSprite(_pressedSprite));
            _button.RegisterCallback<PointerUpEvent>(_ => SetSprite(_normalSprite));
            _button.RegisterCallback<PointerLeaveEvent>(_ => SetSprite(_normalSprite));
        }

        private void ApplySlice(RectOffset slice)
        {
            _button.style.unitySliceLeft = slice.left;
            _button.style.unitySliceRight = slice.right;
            _button.style.unitySliceTop = slice.top;
            _button.style.unitySliceBottom = slice.bottom;
        }

        private void SetSprite(Sprite sprite)
        {
            _button.style.backgroundImage = new StyleBackground(sprite);
        }
    }
}
