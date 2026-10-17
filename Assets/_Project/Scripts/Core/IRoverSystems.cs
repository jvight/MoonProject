namespace MoonProject.Core
{
    /// <summary>
    /// 07's start kit and what is broken on it (docs/design/opening-v2.md §10), registered in the
    /// <see cref="GameContext"/> by Gameplay: the boot readout's lines, and the B6 igniter fit the socket screen on the
    /// CRT offers while 07 stands on the pad it built with the spare igniter in its hold. Every change is also
    /// published as <see cref="Events.RoverSystemChanged"/>.
    /// </summary>
    public interface IRoverSystems
    {
        RoverSystemState StateOf(RoverSystem system);

        /// <summary>The spare igniter is in 07's hold and 07 stands on its pad: the cutter socket takes it.</summary>
        bool CanFitIgniter { get; }

        /// <summary>Fits the spare igniter into the cutter (the cutter turns OK); false when it cannot now.</summary>
        bool TryFitIgniter();
    }
}
