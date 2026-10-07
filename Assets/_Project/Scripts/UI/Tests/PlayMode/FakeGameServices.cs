using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Gameplay;

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>
    /// Everything the UI reads from the World, Rover, Audio and Gameplay domains, as one scriptable stand-in system
    /// (initialised before the UI). Tests set the tether state, the gameplay hint, the wallet, the tower offer and the
    /// radio program.
    /// </summary>
    public sealed class FakeGameServices : MonoBehaviour, IGameSystem, IViewCamera, IAudioSettings, ILookSettings,
        IScrapWallet, ITetherAim, IInteractionHints, IUpgradeShop, IRoverState, IFriendStatuses, IRadioProgram
    {
        private readonly float[] _volumes = { 1f, 1f, 1f, 1f };
        private readonly List<string> _tapes = new List<string>();
        private float _sensitivity = 1f;
        private EventBus _events;

        public Camera Camera { get; set; }

        /// <summary>The one friend in this world (Tilly's definition), its state set by the test.</summary>
        public FriendDefinition Friend { get; set; }

        public FriendStatus TillyStatus { get; set; }

        public Vector3 Position { get; set; }

        public Quaternion Rotation => Quaternion.identity;

        public Vector3 Velocity => Vector3.zero;

        public float Speed => 0f;

        public float NormalizedSpeed => 0f;

        public Vector2 DriveInput => Vector2.zero;

        public bool IsGrounded => true;

        public float AirTime => 0f;

        public Vector3 GroundNormal => Vector3.up;

        public int Count => Friend != null ? 1 : 0;

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

        public bool DialUnlocked { get; set; }

        public RadioChannel Channel { get; set; }

        public string SelectedTape { get; set; } = string.Empty;

        public int OwnedTapeCount => _tapes.Count;

        public int TotalTapeCount { get; set; } = 3;

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
            context.Register<IRoverState>(this);
            context.Register<IFriendStatuses>(this);
            context.Register<IRadioProgram>(this);
        }

        public string GetOwnedTape(int index)
        {
            return _tapes[index];
        }

        /// <summary>07 owns one more tape: the program changes (its card comes from the test's own event).</summary>
        public void AddTape(string cassetteId)
        {
            _tapes.Add(cassetteId);
            _events.Publish(new RadioProgramChanged());
        }

        /// <summary>Bell's dial was turned (or the program otherwise changed): publishes the change.</summary>
        public void Tune(RadioChannel channel, string tape)
        {
            Channel = channel;
            SelectedTape = tape;
            _events.Publish(new RadioProgramChanged());
        }

        public FriendDefinition Definition(int index)
        {
            return Friend;
        }

        public FriendStatus Status(int index)
        {
            return TillyStatus;
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
