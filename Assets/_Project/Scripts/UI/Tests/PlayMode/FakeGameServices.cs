using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Gameplay;

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>
    /// Everything the UI reads from the World, Rover, Audio and Gameplay domains, as one scriptable stand-in system
    /// (initialised before the UI). Tests set the tether state, the gameplay hint, the wallet and the tower offer.
    /// </summary>
    public sealed class FakeGameServices : MonoBehaviour, IGameSystem, IViewCamera, IAudioSettings, ILookSettings,
        IScrapWallet, ITetherAim, IInteractionHints, IUpgradeShop
    {
        private readonly float[] _volumes = { 1f, 1f, 1f, 1f };
        private float _sensitivity = 1f;
        private EventBus _events;

        public Camera Camera { get; set; }

        public UpgradeDefinition Upgrade { get; set; }

        public int UpgradeLevel { get; set; }

        public bool AtStation { get; set; }

        public int Purchases { get; private set; }

        public InteractionHint PrimaryHint { get; set; } = InteractionHint.None;

        public TetherAimState TetherState { get; set; }

        public int Balance { get; private set; }

        public TetherAimState State => TetherState;

        public Vector3 TargetPosition { get; set; }

        public float Strain => 0f;

        public float Length => 0f;

        public float Sensitivity
        {
            get => _sensitivity;
            set => _sensitivity = Mathf.Clamp(value, 0.25f, 3f);
        }

        public bool InvertY { get; set; }

        public bool IsAtStation => StationUpgrade != null;

        public UpgradeDefinition StationUpgrade => AtStation ? Upgrade : null;

        public InteractionHint Primary => IsUpgradeOffered(out InteractionHint upgrade) ? upgrade : PrimaryHint;

        public void Initialize(GameContext context)
        {
            _events = context.Events;
            context.Register<IViewCamera>(this);
            context.Register<IAudioSettings>(this);
            context.Register<ILookSettings>(this);
            context.Register<IScrapWallet>(this);
            context.Register<ITetherAim>(this);
            context.Register<IInteractionHints>(this);
            context.Register<IUpgradeShop>(this);
        }

        /// <summary>Sets the balance and publishes the change like the real wallet.</summary>
        public void SetBalance(int balance)
        {
            int delta = balance - Balance;
            Balance = balance;
            _events.Publish(new CurrencyChanged(balance, delta));
        }

        public bool CanAfford(int cost)
        {
            return Balance >= cost;
        }

        public float GetVolume(AudioBus bus)
        {
            return _volumes[(int)bus];
        }

        public void SetVolume(AudioBus bus, float volume)
        {
            _volumes[(int)bus] = Mathf.Clamp01(volume);
        }

        public bool TryGet(InteractionKind kind, out InteractionHint hint)
        {
            if (kind == InteractionKind.Upgrade)
            {
                return IsUpgradeOffered(out hint);
            }

            hint = PrimaryHint;
            return kind != InteractionKind.None && kind == PrimaryHint.Kind;
        }

        public int LevelOf(string upgradeId)
        {
            return Upgrade != null && upgradeId == Upgrade.Id ? UpgradeLevel : 0;
        }

        public bool TryGetOffer(string upgradeId, out UpgradeOffer offer)
        {
            if (Upgrade == null || upgradeId != Upgrade.Id)
            {
                offer = default;
                return false;
            }

            bool maxed = UpgradeLevel >= Upgrade.MaxLevel;
            offer = new UpgradeOffer(Upgrade, UpgradeLevel, !maxed && CanAfford(Upgrade.Levels[UpgradeLevel].Cost));
            return true;
        }

        public PurchaseResult Purchase(string upgradeId)
        {
            if (!TryGetOffer(upgradeId, out UpgradeOffer offer))
            {
                return PurchaseResult.Unknown;
            }

            if (!AtStation)
            {
                return PurchaseResult.NotAtStation;
            }

            if (offer.IsMaxed)
            {
                return PurchaseResult.Maxed;
            }

            if (!offer.CanAfford)
            {
                return PurchaseResult.CannotAfford;
            }

            Purchases++;
            UpgradeLevel++;
            SetBalance(Balance - offer.NextCost);
            _events.Publish(new UpgradePurchased(upgradeId, UpgradeLevel));
            return PurchaseResult.Purchased;
        }

        private bool IsUpgradeOffered(out InteractionHint hint)
        {
            if (AtStation && Upgrade != null && UpgradeLevel < Upgrade.MaxLevel)
            {
                hint = new InteractionHint(InteractionKind.Upgrade, Vector3.zero,
                    CanAfford(Upgrade.Levels[UpgradeLevel].Cost));
                return true;
            }

            hint = InteractionHint.None;
            return false;
        }
    }
}
