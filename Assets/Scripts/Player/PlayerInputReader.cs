using Game.Core.Events;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    public class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private FloatEventChannelSO _moveDirectionChannel;

        private float _lastDirection;

        private void Update()
        {
            float direction = ReadDirection();
            if (!Mathf.Approximately(direction, _lastDirection))
            {
                _lastDirection = direction;
                _moveDirectionChannel.Raise(direction);
            }
        }

        private float ReadDirection()
        {
            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.isPressed)
            {
                return 0f;
            }

            float pointerX = pointer.position.ReadValue().x;
            return pointerX < Screen.width * 0.5f ? -1f : 1f;
        }
    }
}
