using System;
using MoonProject.Core;
using MoonProject.Core.Input;
using MoonProject.Core.Save;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// Everything the UI reads or drives, resolved once from the GameContext (or handed in by a test). The UI never
    /// mutates game state except through these services.
    /// </summary>
    internal sealed class UiServices
    {
        public UiServices(EventBus events, InputReader input, IViewCamera view, IRoverState rover, IAudioSettings audio,
            ILookSettings look, IFeelSettings feel, ISaveService save, IMaterialStock materials, ITetherAim tether, IInteractionHints hints,
            IUpgradeShop shop, IBlueprintBoard board, IFriendStatuses friends, IRadioProgram radio, IRadioHop hop,
            IRelayStatus relays, ISalvageStatus salvage, IGameFlow flow, ISettingsFile settings, IRoverBattery battery,
            IBeamStatus beam, ITechTree tech, IRecoveryProgress recovery, IRecipeLook recipeLook,
            IWorldLayout world, IRoverAbilities abilities, ICargoHold hold, IMaterialCatalog catalog,
            IDockState dock, IHomeStorage home, IStackTransfer transfer, IRoverEquipment equipment)
        {
            Events = events ?? throw new ArgumentNullException(nameof(events));
            Input = input ?? throw new ArgumentNullException(nameof(input));
            View = view ?? throw new ArgumentNullException(nameof(view));
            Rover = rover ?? throw new ArgumentNullException(nameof(rover));
            Audio = audio ?? throw new ArgumentNullException(nameof(audio));
            Look = look ?? throw new ArgumentNullException(nameof(look));
            Feel = feel ?? throw new ArgumentNullException(nameof(feel));
            Save = save ?? throw new ArgumentNullException(nameof(save));
            Materials = materials ?? throw new ArgumentNullException(nameof(materials));
            Tether = tether ?? throw new ArgumentNullException(nameof(tether));
            Hints = hints ?? throw new ArgumentNullException(nameof(hints));
            Shop = shop ?? throw new ArgumentNullException(nameof(shop));
            Board = board ?? throw new ArgumentNullException(nameof(board));
            Friends = friends ?? throw new ArgumentNullException(nameof(friends));
            Radio = radio ?? throw new ArgumentNullException(nameof(radio));
            Hop = hop ?? throw new ArgumentNullException(nameof(hop));
            Relays = relays ?? throw new ArgumentNullException(nameof(relays));
            Salvage = salvage ?? throw new ArgumentNullException(nameof(salvage));
            Flow = flow ?? throw new ArgumentNullException(nameof(flow));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Battery = battery ?? throw new ArgumentNullException(nameof(battery));
            Tech = tech ?? throw new ArgumentNullException(nameof(tech));
            Recovery = recovery ?? throw new ArgumentNullException(nameof(recovery));
            RecipeLook = recipeLook ?? throw new ArgumentNullException(nameof(recipeLook));
            Beam = beam ?? throw new ArgumentNullException(nameof(beam));
            World = world ?? throw new ArgumentNullException(nameof(world));
            Abilities = abilities ?? throw new ArgumentNullException(nameof(abilities));
            Hold = hold ?? throw new ArgumentNullException(nameof(hold));
            Dock = dock ?? throw new ArgumentNullException(nameof(dock));
            Home = home ?? throw new ArgumentNullException(nameof(home));
            Transfer = transfer ?? throw new ArgumentNullException(nameof(transfer));
            Equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public EventBus Events { get; }

        public InputReader Input { get; }

        public IViewCamera View { get; }

        public IRoverState Rover { get; }

        public IAudioSettings Audio { get; }

        public ILookSettings Look { get; }

        public IFeelSettings Feel { get; }

        public ISaveService Save { get; }

        public IMaterialStock Materials { get; }

        public ITetherAim Tether { get; }

        public IInteractionHints Hints { get; }

        public IUpgradeShop Shop { get; }

        public IBlueprintBoard Board { get; }

        public IFriendStatuses Friends { get; }

        public IRadioProgram Radio { get; }

        public IRadioHop Hop { get; }

        public IRelayStatus Relays { get; }

        public ISalvageStatus Salvage { get; }

        /// <summary>Where home is (the home meter).</summary>
        public IWorldLayout World { get; }

        /// <summary>What 07 has earned (the ability slots).</summary>
        public IRoverAbilities Abilities { get; }

        /// <summary>07's hold: the cargo screen's grid.</summary>
        public ICargoHold Hold { get; }

        /// <summary>Whether 07 rests on the dock: the transfer view is open only then.</summary>
        public IDockState Dock { get; }

        /// <summary>Home's shelves: the transfer view's right side.</summary>
        public IHomeStorage Home { get; }

        /// <summary>Moves one stack between the hold and the shelves.</summary>
        public IStackTransfer Transfer { get; }

        /// <summary>07's equipment: the 07 tab's sockets and parts row.</summary>
        public IRoverEquipment Equipment { get; }

        /// <summary>What each material is called (the pickup feed and the cargo screen).</summary>
        public IMaterialCatalog Catalog { get; }

        public IGameFlow Flow { get; }

        public ISettingsFile Settings { get; }

        /// <summary>07's battery (docs/features/M3-20): the HUD arc reads it.</summary>
        public IRoverBattery Battery { get; }

        /// <summary>Whether 07's beam is at work: the HUD arc hides meanwhile.</summary>
        public IBeamStatus Beam { get; }

        /// <summary>What the station knows and has built: the tech board's labels (docs/features/M3-21).</summary>
        public ITechTree Tech { get; }

        /// <summary>The four recovery targets' lamps: the recovery panel's labels.</summary>
        public IRecoveryProgress Recovery { get; }

        /// <summary>The recipe 07's view rests on (docs/features/M3-24 B).</summary>
        public IRecipeLook RecipeLook { get; }

        /// <summary>
        /// Resolves every service; a missing one throws (a system is missing from the bootstrap order).
        /// </summary>
        public static UiServices From(GameContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return new UiServices(context.Events, context.Input, context.Get<IViewCamera>(), context.Get<IRoverState>(),
                context.Get<IAudioSettings>(), context.Get<ILookSettings>(), context.Get<IFeelSettings>(),
                context.Get<ISaveService>(),
                context.Get<IMaterialStock>(), context.Get<ITetherAim>(), context.Get<IInteractionHints>(),
                context.Get<IUpgradeShop>(), context.Get<IBlueprintBoard>(), context.Get<IFriendStatuses>(), context.Get<IRadioProgram>(),
                context.Get<IRadioHop>(), context.Get<IRelayStatus>(), context.Get<ISalvageStatus>(),
                context.Get<IGameFlow>(), context.Get<ISettingsFile>(), context.Get<IRoverBattery>(),
                context.Get<IBeamStatus>(), context.Get<ITechTree>(), context.Get<IRecoveryProgress>(),
                context.Get<IRecipeLook>(), context.Get<IWorldLayout>(), context.Get<IRoverAbilities>(),
                context.Get<ICargoHold>(), context.Get<IMaterialCatalog>(), context.Get<IDockState>(),
                context.Get<IHomeStorage>(), context.Get<IStackTransfer>(), context.Get<IRoverEquipment>());
        }
    }
}
