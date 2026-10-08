using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Gameplay;

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>
    /// Everything the UI reads from the World, Rover, Audio and Gameplay domains, as one scriptable stand-in system
    /// (initialised before the UI). Tests set the tether state, the gameplay hint, the material stock, the tower offer,
    /// the radio program, the relay network, the radio-hop and the salvage cut.
    /// </summary>
    public sealed class FakeGameServices : MonoBehaviour, IGameSystem, IViewCamera, IAudioSettings, ILookSettings,
        IMaterialStock, ITetherAim, IInteractionHints, IUpgradeShop, IRoverState, IFriendStatuses, IRadioProgram,
        IRadioHop, IRelayStatus, ISalvageStatus
    {
        private readonly float[] _volumes = { 1f, 1f, 1f, 1f };
        private readonly List<string> _tapes = new List<string>();
        private readonly List<string> _hopChoices = new List<string>();
        private readonly Dictionary<string, int> _levels = new Dictionary<string, int>();
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

        /// <summary>The station's one upgrade (the radio tower), unless <see cref="Bench"/> is set.</summary>
        public UpgradeDefinition Upgrade { get; set; }

        /// <summary>When set, the station sells these instead, in this order (Kenji's bench).</summary>
        public UpgradeDefinition[] Bench { get; set; }

        public bool AtStation { get; set; }

        public int Purchases { get; private set; }

        public InteractionHint PrimaryHint { get; set; } = InteractionHint.None;

        public TetherAimState TetherState { get; set; }

        public int Metal { get; private set; }

        public int Wiring { get; private set; }

        public int Optics { get; private set; }

        public int Total => Metal + Wiring + Optics;

        public bool HasTarget { get; set; }

        public Vector3 CutPoint { get; set; }

        public bool IsCutting { get; set; }

        /// <summary>The salvage cut's progress (<see cref="ISalvageStatus.Progress"/>; the hop has its own).</summary>
        public float CutProgress { get; set; }

        public SalvageMaterial Material { get; set; }

        float ISalvageStatus.Progress => CutProgress;

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

        public UpgradeDefinition StationUpgrade
        {
            get
            {
                if (StationUpgradeCount == 0)
                {
                    return null;
                }

                for (int i = 0; i < StationUpgradeCount; i++)
                {
                    UpgradeDefinition upgrade = StationUpgradeAt(i);
                    if (LevelOf(upgrade.Id) < upgrade.MaxLevel)
                    {
                        return upgrade;
                    }
                }

                return StationUpgradeAt(StationUpgradeCount - 1);
            }
        }

        public int StationUpgradeCount => !AtStation ? 0 : Bench?.Length ?? (Upgrade != null ? 1 : 0);

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
            context.Register<ISalvageStatus>(this);
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

        /// <summary>Sets the materials held and publishes the change like the real stock.</summary>
        public void SetMaterials(int metal, int wiring, int optics)
        {
            Metal = metal;
            Wiring = wiring;
            Optics = optics;
            _events.Publish(new MaterialsChanged(metal, wiring, optics));
        }

        public int Of(SalvageMaterial material)
        {
            switch (material)
            {
                case SalvageMaterial.Metal:
                    return Metal;
                case SalvageMaterial.Wiring:
                    return Wiring;
                case SalvageMaterial.Optics:
                    return Optics;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(material), material, "Unknown material.");
            }
        }

        public bool Has(Recipe recipe)
        {
            return Metal >= recipe.Metal && Wiring >= recipe.Wiring && Optics >= recipe.Optics;
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

        public UpgradeDefinition StationUpgradeAt(int index)
        {
            return Bench != null ? Bench[index] : Upgrade;
        }

        public int LevelOf(string upgradeId)
        {
            return _levels.TryGetValue(upgradeId, out int level) ? level : 0;
        }

        public bool TryGetOffer(string upgradeId, out UpgradeOffer offer)
        {
            UpgradeDefinition upgrade = Sold(upgradeId);
            if (upgrade == null)
            {
                offer = default;
                return false;
            }

            int level = LevelOf(upgradeId);
            bool maxed = level >= upgrade.MaxLevel;
            offer = new UpgradeOffer(upgrade, level, !maxed && Has(upgrade.Levels[level].Recipe));
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
            _levels[upgradeId] = offer.CurrentLevel + 1;
            Recipe cost = offer.NextCost;
            SetMaterials(Metal - cost.Metal, Wiring - cost.Wiring, Optics - cost.Optics);
            _events.Publish(new UpgradePurchased(upgradeId, offer.CurrentLevel + 1));
            return PurchaseResult.Purchased;
        }

        private bool IsUpgradeOffered(out InteractionHint hint)
        {
            UpgradeDefinition upgrade = StationUpgrade;
            int level = upgrade != null ? LevelOf(upgrade.Id) : 0;
            if (upgrade != null && level < upgrade.MaxLevel)
            {
                hint = new InteractionHint(InteractionKind.Upgrade, Vector3.zero, Has(upgrade.Levels[level].Recipe));
                return true;
            }

            hint = InteractionHint.None;
            return false;
        }

        /// <summary>The upgrade called <paramref name="upgradeId"/> if this station's catalogue has it.</summary>
        private UpgradeDefinition Sold(string upgradeId)
        {
            UpgradeDefinition[] bench = Bench;
            if (bench != null)
            {
                for (int i = 0; i < bench.Length; i++)
                {
                    if (bench[i].Id == upgradeId)
                    {
                        return bench[i];
                    }
                }

                return null;
            }

            return Upgrade != null && Upgrade.Id == upgradeId ? Upgrade : null;
        }
    }
}
