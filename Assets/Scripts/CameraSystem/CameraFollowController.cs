using Game.Data;
using UnityEngine;

namespace Game.CameraSystem
{
    public class CameraFollowController : MonoBehaviour
    {
        [SerializeField] private GameConfigSO _config;

        private Transform _target;
        private float _verticalVelocity;

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            var position = transform.position;
            float targetY = Mathf.Max(position.y, _target.position.y);
            float smoothedY = Mathf.SmoothDamp(position.y, targetY, ref _verticalVelocity, _config.CameraFollowSmoothTime);

            if (targetY - smoothedY > _config.MaxCameraLag)
            {
                smoothedY = targetY - _config.MaxCameraLag;
            }

            position.y = smoothedY;
            transform.position = position;
        }
    }
}
