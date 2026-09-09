using Game.Core.Utility;
using Game.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Player
{
    public class PlayerFallDetector : MonoBehaviour
    {
        [SerializeField] private GameConfigSO _config;

        private Camera _mainCamera;
        private bool _hasTriggeredRestart;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            if (_hasTriggeredRestart)
            {
                return;
            }

            var bounds = ScreenBoundsUtility.GetWorldBounds(_mainCamera);
            if (transform.position.y < bounds.yMin - _config.FallDeathMargin)
            {
                _hasTriggeredRestart = true;
                RestartGame();
            }
        }

        private static void RestartGame()
        {
            var activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);
        }
    }
}
