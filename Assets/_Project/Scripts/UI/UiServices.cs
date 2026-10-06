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
            ILookSettings look, ISaveService save, IScrapWallet wallet, ITetherAim tether, IInteractionHints hints,
            IUpgradeShop shop, IFriendStatuses friends)
        {
            Events = events ?? throw new ArgumentNullException(nameof(events));
            Input = input ?? throw new ArgumentNullException(nameof(input));
            View = view ?? throw new ArgumentNullException(nameof(view));
            Rover = rover ?? throw new ArgumentNullException(nameof(rover));
            Audio = audio ?? throw new ArgumentNullException(nameof(audio));
            Look = look ?? throw new ArgumentNullException(nameof(look));
            Save = save ?? throw new ArgumentNullException(nameof(save));
            Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            Tether = tether ?? throw new ArgumentNullException(nameof(tether));
            Hints = hints ?? throw new ArgumentNullException(nameof(hints));
            Shop = shop ?? throw new ArgumentNullException(nameof(shop));
            Friends = friends ?? throw new ArgumentNullException(nameof(friends));
        }

        public EventBus Events { get; }

        public InputReader Input { get; }

        public IViewCamera View { get; }

        public IRoverState Rover { get; }

        public IAudioSettings Audio { get; }

        public ILookSettings Look { get; }

        public ISaveService Save { get; }

        public IScrapWallet Wallet { get; }

        public ITetherAim Tether { get; }

        public IInteractionHints Hints { get; }

        public IUpgradeShop Shop { get; }

        public IFriendStatuses Friends { get; }

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
                context.Get<IAudioSettings>(), context.Get<ILookSettings>(), context.Get<ISaveService>(),
                context.Get<IScrapWallet>(), context.Get<ITetherAim>(), context.Get<IInteractionHints>(),
                context.Get<IUpgradeShop>(), context.Get<IFriendStatuses>());
        }
    }
}
