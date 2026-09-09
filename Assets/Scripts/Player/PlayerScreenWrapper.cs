using Game.Core.Utility;
using Game.Data;
using UnityEngine;

namespace Game.Player
{
    public class PlayerScreenWrapper : MonoBehaviour
    {
        [SerializeField] private GameConfigSO _config;

        private Camera _mainCamera;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            var bounds = ScreenBoundsUtility.GetWorldBounds(_mainCamera);
            var position = transform.position;

            if (position.x < bounds.xMin - _config.ScreenWrapMargin)
            {
                position.x = bounds.xMax + _config.ScreenWrapMargin;
            }
            else if (position.x > bounds.xMax + _config.ScreenWrapMargin)
            {
                position.x = bounds.xMin - _config.ScreenWrapMargin;
            }

            transform.position = position;
        }
    }
}
