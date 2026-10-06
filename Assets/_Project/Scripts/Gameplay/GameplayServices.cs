using System;
using MoonProject.Core;
using MoonProject.Core.Input;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// What every gameplay part works with, resolved once by <see cref="GameplaySystem"/> from the GameContext and
    /// handed to each part's Initialize. Parts never reach into the context themselves.
    /// </summary>
    public sealed class GameplayServices
    {
        public GameplayServices(EventBus events, InputReader input, ITerrainQuery terrain, IWorldLayout layout,
            IRoverState rover, IRoverRig rig, IViewCamera view, ScrapWallet wallet, GameplayVisuals visuals,
            GlowMeshSet meshes)
        {
            Events = events ?? throw new ArgumentNullException(nameof(events));
            Input = input ?? throw new ArgumentNullException(nameof(input));
            Terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            Rover = rover ?? throw new ArgumentNullException(nameof(rover));
            Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            View = view ?? throw new ArgumentNullException(nameof(view));
            Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            Visuals = visuals != null ? visuals : throw new ArgumentNullException(nameof(visuals));
            Meshes = meshes ?? throw new ArgumentNullException(nameof(meshes));
        }

        public EventBus Events { get; }

        public InputReader Input { get; }

        public ITerrainQuery Terrain { get; }

        public IWorldLayout Layout { get; }

        public IRoverState Rover { get; }

        public IRoverRig Rig { get; }

        public IViewCamera View { get; }

        public ScrapWallet Wallet { get; }

        public GameplayVisuals Visuals { get; }

        public GlowMeshSet Meshes { get; }
    }
}
