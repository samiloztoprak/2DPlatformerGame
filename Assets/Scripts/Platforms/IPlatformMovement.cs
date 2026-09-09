using Game.Data;
using UnityEngine;

namespace Game.Platforms
{
    public interface IPlatformMovement
    {
        Vector3 Evaluate(Vector3 basePosition, float elapsedTime, PlatformDataSO data);
    }
}
