namespace MoonProject.Core
{
    /// <summary>
    /// Whether 07 is up in the crew's lab and which station the turntable faces (docs/features/M3-19), registered in
    /// the <see cref="GameContext"/> by the Gameplay domain and announced with <see cref="Events.LabEntered"/>,
    /// <see cref="Events.LabLeft"/> and <see cref="Events.LabStationFaced"/>. Allocation-free: safe to poll every frame.
    /// </summary>
    public interface ILabInterior
    {
        /// <summary>True from the moment the platform settles in the cabin until it starts down.</summary>
        bool IsInside { get; }

        /// <summary>The station the turntable faces or is turning to; meaningless while not inside.</summary>
        LabStation Facing { get; }
    }
}
