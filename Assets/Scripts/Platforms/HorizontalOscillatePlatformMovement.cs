using Game.Data;
using UnityEngine;

namespace Game.Platforms
{
    public class HorizontalOscillatePlatformMovement : IPlatformMovement
    {
        public Vector3 Evaluate(Vector3 basePosition, float elapsedTime, PlatformDataSO data)
        {
            float offsetX = Mathf.Sin(elapsedTime * data.MoveSpeed) * data.MoveRange;
            return basePosition + new Vector3(offsetX, 0f, 0f);
        }
    }
}
