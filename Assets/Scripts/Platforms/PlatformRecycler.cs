using System.Collections.Generic;

namespace Game.Platforms
{
    public class PlatformRecycler
    {
        private readonly PlatformPoolManager _poolManager;

        public PlatformRecycler(PlatformPoolManager poolManager)
        {
            _poolManager = poolManager;
        }

        public void RecycleBelow(List<PlatformController> activePlatforms, float thresholdY)
        {
            for (int i = activePlatforms.Count - 1; i >= 0; i--)
            {
                var platform = activePlatforms[i];
                if (platform.transform.position.y < thresholdY)
                {
                    activePlatforms.RemoveAt(i);
                    _poolManager.Release(platform);
                }
            }
        }
    }
}
