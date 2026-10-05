namespace MoonProject.Core
{
    /// <summary>
    /// A scene-level system wired by GameBootstrap. <see cref="Initialize"/> is called exactly once, in the order
    /// listed on the bootstrap, before any Start(). Register services here; resolve services registered by systems
    /// earlier in the order.
    /// </summary>
    public interface IGameSystem
    {
        void Initialize(GameContext context);
    }
}
