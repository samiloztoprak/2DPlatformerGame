using UnityEngine;

namespace Game.Core.Utility
{
    public static class ScreenBoundsUtility
    {
        public static Rect GetWorldBounds(Camera camera)
        {
            float halfHeight = camera.orthographicSize;
            float halfWidth = halfHeight * camera.aspect;
            Vector3 center = camera.transform.position;

            return new Rect(center.x - halfWidth, center.y - halfHeight, halfWidth * 2f, halfHeight * 2f);
        }
    }
}
