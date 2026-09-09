using Game.Data;
using UnityEngine;

namespace Game.Platforms
{
    public class StaticPlatformMovement : IPlatformMovement
    {
        public Vector3 Evaluate(Vector3 basePosition, float elapsedTime, PlatformDataSO data)
        {
            return basePosition;
        }
    }
}
