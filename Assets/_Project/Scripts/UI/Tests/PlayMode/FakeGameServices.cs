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
    /// (initialised before the UI). Tests set the gameplay hint, the material stock, the station offer, the salvage
    /// cut, the battery and the beam at work.
    /// </summary>
    public sealed class FakeGameServices : MonoBehaviour, IGameSystem, IViewCamera, IAudioSettings, ILookSettings, IFeelSettings,
        IMaterialStock, IInteractionHints, IUpgradeShop, IRoverState, ISalvageStatus, IRoverBattery, IBeamStatus,
        IRecoveryProgress, IRecipeLook
    {
        private readonly int[] _litLamps = new int[Enum.GetValues(typeof(RecoveryTarget)).Length];
        private readonly int[] _lamps = { 3, 3, 4, 1 };

        private readonly float[] _volumes = { 1f, 1f, 1f, 1f };
        private readonly Dictionary<string, int> _levels = new Dictionary<string, int>();
        private float _sensitivity = 1f;
        private EventBus _events;

        public Camera Camera { get; set; }

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

        public float RumbleStrength { get; set; } = 1f;

        /// <summary>True while the next level waits for a component the tower has not been fed (the coil).</summary>
        public bool AwaitingPart { get; set; }

        public int Purchases { get; private set; }

        public InteractionHint PrimaryHint { get; set; } = InteractionHint.None;

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

        /// <summary>The bay refuses a kit: the parts row is full.</summary>
        public bool PartsRowFull { get; set; }

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

        /// <summary>The salvage cut's progress (<see cref="ISalvageStatus.Progress"/>).</summary>
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
            context.Register<IInteractionHints>(this);
            context.Register<IUpgradeShop>(this);
            context.Register<IRecipeLook>(this);
            context.Register<IRoverState>(this);
            context.Register<ISalvageStatus>(this);
            context.Register<IRoverBattery>(this);
            context.Register<IBeamStatus>(this);
            context.Register<IRecoveryProgress>(this);
            context.Register<ICargoHold>(Hold);
            context.Register<IMaterialCatalog>(Catalog);
            context.Register<IHomeStorage>(Home);
            context.Register<IDockState>(Dock);
            context.Register<IRoverEquipment>(Equipment);
            context.Register<IRoverPortrait>(Portrait);
            context.Register<IStackTransfer>(Transfer);
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

            if (PartsRowFull)
            {
                return PurchaseResult.RowFull;
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
