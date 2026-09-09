using Game.Data;

namespace Game.Platforms
{
    public static class PlatformMovementFactory
    {
        public static IPlatformMovement Create(PlatformMovementType type)
        {
            return type switch
            {
                PlatformMovementType.HorizontalOscillate => new HorizontalOscillatePlatformMovement(),
                _ => new StaticPlatformMovement(),
            };
        }
    }
}
