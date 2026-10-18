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
        IMaterialStock, IInteractionHints, IRoverState, ISalvageStatus, IRoverBattery, IBeamStatus
    {
        private readonly float[] _volumes = { 1f, 1f, 1f, 1f };
        private float _sensitivity = 1f;
        private EventBus _events;

        public Camera Camera { get; set; }

        public Vector3 Position { get; set; }

        public Quaternion Rotation => Quaternion.identity;

        public Vector3 Velocity => Vector3.zero;

        /// <summary>07's speed (m/s), set by a test (0: standing still).</summary>
        public float Speed { get; set; }

        public float NormalizedSpeed => 0f;

        public Vector2 DriveInput => Vector2.zero;

        public bool IsGrounded => true;

        public float AirTime => 0f;

        public Vector3 GroundNormal => Vector3.up;

        public bool RumbleEnabled { get; set; } = true;

        public float RumbleStrength { get; set; } = 1f;

        public InteractionHint PrimaryHint { get; set; } = InteractionHint.None;

        private readonly int[] _units = new int[SalvageMaterials.Count];

        /// <summary>07's hold, set by a test.</summary>
        public FakeHold Hold { get; } = new FakeHold();

        public FakeCatalog Catalog { get; } = new FakeCatalog();

        /// <summary>07's systems, set by a test.</summary>
        public FakeSystems Systems { get; } = new FakeSystems();

        /// <summary>The sky, set by a test.</summary>
        public FakeSunlight Sun { get; } = new FakeSunlight();

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

        public InteractionHint Primary => PrimaryHint;

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
            context.Register<IRoverState>(this);
            context.Register<ISalvageStatus>(this);
            context.Register<IRoverBattery>(this);
            context.Register<IBeamStatus>(this);
            context.Register<ICargoHold>(Hold);
            context.Register<IMaterialCatalog>(Catalog);
            context.Register<IRoverSystems>(Systems);
            context.Register<ISunlight>(Sun);
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
            hint = PrimaryHint;
            return kind != InteractionKind.None && kind == PrimaryHint.Kind;
        }
    }
}
