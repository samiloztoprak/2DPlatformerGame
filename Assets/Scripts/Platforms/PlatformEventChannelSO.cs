using Game.Core.Events;
using UnityEngine;

namespace Game.Platforms
{
    [CreateAssetMenu(menuName = "Events/Platform Channel", fileName = "PlatformEventChannel")]
    public class PlatformEventChannelSO : EventChannelSO<PlatformController>
    {
    }
}
