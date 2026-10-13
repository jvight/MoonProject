using System;
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
    /// the radio program, the relay network, the radio-hop, the salvage cut, the battery and the beam at work.
    /// </summary>
    public sealed class FakeGameServices : MonoBehaviour, IGameSystem, IViewCamera, IAudioSettings, ILookSettings, IFeelSettings,
        IMaterialStock, ITetherAim, IInteractionHints, IUpgradeShop, IRoverState, IFriendStatuses, IRadioProgram,
        IRadioHop, IRelayStatus, ISalvageStatus, IBlueprintBoard, IRoverBattery, IBeamStatus, ITechTree,
        IRecoveryProgress, IRecipeLook, IWorldLayout, IRoverAbilities
    {
        private readonly TechNodeState[] _techStates = new TechNodeState[Enum.GetValues(typeof(TechNodeId)).Length];
        private readonly int[] _litLamps = new int[Enum.GetValues(typeof(RecoveryTarget)).Length];
        private readonly int[] _lamps = { 3, 3, 4, 1 };

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

        /// <summary>The recipe the view rests on (<see cref="IRecipeLook"/>), set by a test.</summary>
        public string Looked { get; set; }

        public bool IsSheet { get; set; }

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

        /// <summary>The station's one upgrade (the radio tower), unless <see cref="Bay"/> is set.</summary>
        public UpgradeDefinition Upgrade { get; set; }

        /// <summary>When set, the station sells these instead, in this order (Kenji's Rover Bay).</summary>
        public UpgradeDefinition[] Bay { get; set; }

        public bool AtStation { get; set; }

        /// <summary>
        /// False in captures over the real scene: a staged purchase must not set the scene's real stations working.
        /// </summary>
        public bool AnnouncesPurchases { get; set; } = true;

        public bool RumbleEnabled { get; set; } = true;

        /// <summary>Where home is, for the home meter (the test's origin unless set).</summary>
        public Vector3 BasePosition { get; set; }

        public Vector3 PeakPosition { get; set; } = new Vector3(0f, 200f, 2000f);

        public Vector3 EarthDirection { get; set; } = Vector3.up;

        /// <summary>The abilities 07 has, for the ability slots.</summary>
        public HashSet<RoverAbility> Abilities { get; } = new HashSet<RoverAbility>();

        public bool Has(RoverAbility ability)
        {
            return Abilities.Contains(ability);
        }

        public void Grant(RoverAbility ability)
        {
            Abilities.Add(ability);
        }

        public void Revoke(RoverAbility ability)
        {
            Abilities.Remove(ability);
        }

        public float RumbleStrength { get; set; } = 1f;

        /// <summary>True while the next level waits for a component the tower has not been fed (the coil).</summary>
        public bool AwaitingPart { get; set; }

        public int Purchases { get; private set; }

        public InteractionHint PrimaryHint { get; set; } = InteractionHint.None;

        public TetherAimState TetherState { get; set; }

        private readonly int[] _units = new int[SalvageMaterials.Count];

        /// <summary>07's hold, set by a test.</summary>
        public FakeHold Hold { get; } = new FakeHold();

        public FakeCatalog Catalog { get; } = new FakeCatalog();

        /// <summary>Home's shelves, set by a test.</summary>
        public FakeHome Home { get; } = new FakeHome();

        /// <summary>Whether 07 rests on the dock, set by a test.</summary>
        public FakeDock Dock { get; } = new FakeDock();

        /// <summary>07's equipment, set by a test.</summary>
        public FakeEquipment Equipment { get; } = new FakeEquipment();

        /// <summary>07's portrait, set by a test.</summary>
        public FakePortrait Portrait { get; } = new FakePortrait();

        /// <summary>The docked transfer, moving whole slots between <see cref="Hold"/> and <see cref="Home"/>.</summary>
        public FakeTransfer Transfer => _transfer ?? (_transfer = new FakeTransfer(Hold, Home, Dock));

        private FakeTransfer _transfer;

        private void OnDestroy()
        {
            Portrait.Dispose();
        }

        public int Metal => _units[(int)SalvageMaterial.Metal];

        public int Wiring => _units[(int)SalvageMaterial.Wiring];

        public int Optics => _units[(int)SalvageMaterial.Optics];

        public int Total
        {
            get
            {
                int total = 0;
                foreach (int units in _units)
                {
                    total += units;
                }

                return total;
            }
        }

        public bool HasTarget { get; set; }

        public Vector3 CutPoint { get; set; }

        public bool IsCutting { get; set; }

        /// <summary>The salvage cut's progress (<see cref="ISalvageStatus.Progress"/>; the hop has its own).</summary>
        public float CutProgress { get; set; }

        public SalvageMaterial Material { get; set; }

        float ISalvageStatus.Progress => CutProgress;

        public bool IsBeamCharging { get; set; }

        public float BeamChargeProgress { get; set; }

        public Vector3 HotPoint { get; set; }

        public Vector3 CutNormal { get; set; } = Vector3.up;

        public int CutLineCount => 0;

        public Vector3 CutLinePoint(int index)
        {
            return HotPoint;
        }

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

        public bool GentleCamera { get; set; }

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

        public int StationUpgradeCount => !AtStation ? 0 : Bay?.Length ?? (Upgrade != null ? 1 : 0);

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

        public int Cells { get; set; } = 4;

        public float Charge { get; set; } = 4f;

        public float Charge01 => Charge / Cells;

        public bool IsLowPower { get; set; }

        public float DriveCap01 => 1f;

        public bool CanLeap => true;

        public float Drums01 => 0f;

        public bool IsCharging { get; set; }

        public ChargerKind Charger => ChargerKind.Dock;

        /// <summary>The beam is at work (the cell row dims).</summary>
        public bool Working { get; set; }

        public void Initialize(GameContext context)
        {
            _events = context.Events;
            context.Register<IViewCamera>(this);
            context.Register<IAudioSettings>(this);
            context.Register<ILookSettings>(this);
            context.Register<IFeelSettings>(this);
            context.Register<IMaterialStock>(this);
            context.Register<ITetherAim>(this);
            context.Register<IInteractionHints>(this);
            context.Register<IUpgradeShop>(this);
            context.Register<IBlueprintBoard>(this);
            context.Register<IRecipeLook>(this);
            context.Register<IRoverState>(this);
            context.Register<IFriendStatuses>(this);
            context.Register<IRadioProgram>(this);
            context.Register<IRadioHop>(this);
            context.Register<IRelayStatus>(this);
            context.Register<ISalvageStatus>(this);
            context.Register<IRoverBattery>(this);
            context.Register<IBeamStatus>(this);
            context.Register<ITechTree>(this);
            context.Register<IRecoveryProgress>(this);
            context.Register<IWorldLayout>(this);
            context.Register<IRoverAbilities>(this);
            context.Register<ICargoHold>(Hold);
            context.Register<IMaterialCatalog>(Catalog);
            context.Register<IHomeStorage>(Home);
            context.Register<IDockState>(Dock);
            context.Register<IRoverEquipment>(Equipment);
            context.Register<IRoverPortrait>(Portrait);
            context.Register<IStackTransfer>(Transfer);
        }

        public int KnownCount
        {
            get
            {
                int known = 0;
                foreach (TechNodeState state in _techStates)
                {
                    known += state != TechNodeState.Unknown ? 1 : 0;
                }

                return known;
            }
        }

        public TechNodeState StateOf(TechNodeId node)
        {
            return _techStates[(int)node];
        }

        /// <summary>Sets what the tech board knows of <paramref name="node"/>.</summary>
        public void SetTech(TechNodeId node, TechNodeState state)
        {
            _techStates[(int)node] = state;
        }

        public int LitLamps(RecoveryTarget target)
        {
            return _litLamps[(int)target];
        }

        public int Lamps(RecoveryTarget target)
        {
            return _lamps[(int)target];
        }

        public bool IsComplete(RecoveryTarget target)
        {
            return _litLamps[(int)target] >= _lamps[(int)target];
        }

        /// <summary>Sets how many lamps of <paramref name="target"/>'s row are lit.</summary>
        public void SetLitLamps(RecoveryTarget target, int lit)
        {
            _litLamps[(int)target] = lit;
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
            _units[(int)SalvageMaterial.Metal] = metal;
            _units[(int)SalvageMaterial.Wiring] = wiring;
            _units[(int)SalvageMaterial.Optics] = optics;
            _events.Publish(new MaterialsChanged(metal, wiring, optics));
        }

        /// <summary>Sets the units of any material (the three salvage ones also publish MaterialsChanged).</summary>
        public void SetUnits(SalvageMaterial material, int units)
        {
            _units[(int)material] = units;
            if ((int)material < Materials.RecipeCount)
            {
                _events.Publish(new MaterialsChanged(Metal, Wiring, Optics));
            }
        }

        public int Of(SalvageMaterial material)
        {
            return _units[(int)material];
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
            return Bay != null ? Bay[index] : Upgrade;
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

        /// <summary>Kits the bay has no blueprint for yet (every other kit is pinned).</summary>
        public HashSet<string> Unpinned { get; } = new HashSet<string>(StringComparer.Ordinal);

        public bool IsPinned(string upgradeId)
        {
            return !Unpinned.Contains(upgradeId);
        }

        public string ComponentNeeded(string upgradeId)
        {
            return AwaitingPart ? "Part_TowerCoil" : null;
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

            if (AwaitingPart)
            {
                return PurchaseResult.NeedsPart;
            }

            Purchases++;
            _levels[upgradeId] = offer.CurrentLevel + 1;
            Recipe cost = offer.NextCost;
            SetMaterials(Metal - cost.Metal, Wiring - cost.Wiring, Optics - cost.Optics);
            if (AnnouncesPurchases)
            {
                _events.Publish(new UpgradePurchased(upgradeId, offer.CurrentLevel + 1));
            }
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
            UpgradeDefinition[] bay = Bay;
            if (bay != null)
            {
                for (int i = 0; i < bay.Length; i++)
                {
                    if (bay[i].Id == upgradeId)
                    {
                        return bay[i];
                    }
                }

                return null;
            }

            return Upgrade != null && Upgrade.Id == upgradeId ? Upgrade : null;
        }
    }
}
