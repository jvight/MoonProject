using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Gameplay;

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>
    /// Everything the UI reads from the World, Rover, Audio and Gameplay domains, as one scriptable stand-in system
    /// (initialised before the UI). Tests set the tether state, the gameplay hint, the wallet, the tower offer, the
    /// radio program, the relay network and the radio-hop.
    /// </summary>
    public sealed class FakeGameServices : MonoBehaviour, IGameSystem, IViewCamera, IAudioSettings, ILookSettings,
        IMaterialStock, ITetherAim, IInteractionHints, IUpgradeShop, IRoverState, IFriendStatuses, IRadioProgram,
        IRadioHop, IRelayStatus
    {
        private readonly float[] _volumes = { 1f, 1f, 1f, 1f };
        private readonly List<string> _tapes = new List<string>();
        private readonly List<string> _hopChoices = new List<string>();
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

        /// <summary>Material units the fake holds, all counted as metal (it compares totals only).</summary>
        public int Balance { get; private set; }

        public int Metal => Balance;

        public int Wiring => 0;

        public int Optics => 0;

        public int Total => Balance;

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

        public RadioHopPhase Phase { get; set; }

        public bool CanOpen => Phase == RadioHopPhase.Closed && _hopChoices.Count > 0;

        public int Here => -1;

        public int ChoiceCount => Phase == RadioHopPhase.Closed ? 0 : _hopChoices.Count;

        public int Selected { get; set; }

        public float ConfirmHold { get; set; }

        public float Fade { get; set; }

        public float Progress => 0f;

        public int MastCount { get; set; } = 4;

        public int LitMasts { get; set; }

        public Recipe NextCost { get; set; } = new Recipe(2, 1, 0);

        public float RestoreHold { get; set; }

        public void Initialize(GameContext context)
        {
            _events = context.Events;
            context.Register<IViewCamera>(this);
            context.Register<IAudioSettings>(this);
            context.Register<ILookSettings>(this);
            context.Register<IMaterialStock>(this);
            context.Register<ITetherAim>(this);
            context.Register<IInteractionHints>(this);
            context.Register<IUpgradeShop>(this);
            context.Register<IRoverState>(this);
            context.Register<IFriendStatuses>(this);
            context.Register<IRadioProgram>(this);
            context.Register<IRadioHop>(this);
            context.Register<IRelayStatus>(this);
        }

        /// <summary>The lit nodes the list will offer (their name keys), home first.</summary>
        public void SetHopChoices(params string[] labelKeys)
        {
            _hopChoices.Clear();
            _hopChoices.AddRange(labelKeys);
        }

        public int ChoiceNode(int choice)
        {
            return choice;
        }

        public string ChoiceLabelKey(int choice)
        {
            return _hopChoices[choice];
        }

        public bool Open()
        {
            if (!CanOpen)
            {
                return false;
            }

            Selected = 0;
            Phase = RadioHopPhase.Choosing;
            _events.Publish(new RadioHopListChanged(true));
            return true;
        }

        public void Next()
        {
            Selected = (Selected + 1) % _hopChoices.Count;
        }

        public void Previous()
        {
            Selected = (Selected + _hopChoices.Count - 1) % _hopChoices.Count;
        }

        public bool Confirm()
        {
            if (Phase != RadioHopPhase.Choosing)
            {
                return false;
            }

            Phase = RadioHopPhase.Leaving;
            _events.Publish(new RadioHopListChanged(false));
            return true;
        }

        public void Cancel()
        {
            if (Phase == RadioHopPhase.Choosing)
            {
                Phase = RadioHopPhase.Closed;
                _events.Publish(new RadioHopListChanged(false));
            }
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

        /// <summary>Sets the units held and publishes the change like the real stock.</summary>
        public void SetBalance(int balance)
        {
            Balance = balance;
            _events.Publish(new MaterialsChanged(balance, 0, 0));
        }

        public int Of(SalvageMaterial material)
        {
            return material == SalvageMaterial.Metal ? Balance : 0;
        }

        /// <summary>The fake affords a recipe when it holds as many units in total.</summary>
        public bool Has(Recipe recipe)
        {
            return Balance >= recipe.Total;
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
            offer = new UpgradeOffer(Upgrade, UpgradeLevel, !maxed && Has(Upgrade.Levels[UpgradeLevel].Recipe));
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
            SetBalance(Balance - offer.NextCost.Total);
            _events.Publish(new UpgradePurchased(upgradeId, UpgradeLevel));
            return PurchaseResult.Purchased;
        }

        private bool IsUpgradeOffered(out InteractionHint hint)
        {
            if (AtStation && Upgrade != null && UpgradeLevel < Upgrade.MaxLevel)
            {
                hint = new InteractionHint(InteractionKind.Upgrade, Vector3.zero,
                    Has(Upgrade.Levels[UpgradeLevel].Recipe));
                return true;
            }

            hint = InteractionHint.None;
            return false;
        }
    }
}
