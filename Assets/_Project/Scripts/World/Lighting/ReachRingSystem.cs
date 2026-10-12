using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.World
{
    /// <summary>
    /// Shows a reach growing (polish wave 1): a warm ring rolls out over the ground when the tower's clear-signal
    /// radius grows (<see cref="SignalRadiusChanged"/>, from home), a relay mast lights (<see cref="RelayRestored"/>,
    /// from the mast) or a new cell pack fits (<see cref="BatteryChanged"/> with more cells, from 07). Loads and the
    /// first report of each are silent. During the tower's build beat (feel-pass §8.2) the tower's ring waits for the
    /// beat's Follow (strength 1), or a timeout. LofiTerrain draws the ring from two shader globals; thin: the timing
    /// lives in <see cref="ReachRing"/>.
    /// </summary>
    public sealed class ReachRingSystem : MonoBehaviour, IGameSystem
    {
        private static readonly int RingId = Shader.PropertyToID("_MoonReachRing");
        private static readonly int RingColorId = Shader.PropertyToID("_MoonReachRingColor");

        [Tooltip("World tuning (Assets/_Project/Data/Tuning/WorldSettings.asset); its Reach Ring block.")]
        [SerializeField] private WorldSettings _settings;

        private GameContext _context;
        private ReachRing _ring;
        private IDisposable _signal;
        private IDisposable _relay;
        private IDisposable _battery;
        private float _signalRadius = -1f;
        private int _cells = -1;
        private bool _showing;
        private IDisposable _feel;
        private bool _buildPending;
        private float _buildAt = float.NegativeInfinity;
        private bool _ringHeld;
        private Vector3 _heldCentre;
        private float _heldReach;

        // A Follow above this strength is the reach ring's cue (strength 0 is the cable race's).
        private const float RingFollowStrength = 0.5f;

        public void Initialize(GameContext context)
        {
            if (_settings == null)
            {
                Debug.LogError($"{nameof(ReachRingSystem)}: World Settings is not assigned.", this);
                enabled = false;
                return;
            }

            _context = context;
            _ring = new ReachRing(_settings.ReachRing);
            _signal = context.Events.Subscribe<SignalRadiusChanged>(OnSignalRadius);
            _relay = context.Events.Subscribe<RelayRestored>(OnRelayRestored);
            _battery = context.Events.Subscribe<BatteryChanged>(OnBatteryChanged);
            _feel = context.Events.Subscribe<FeelBeat>(OnFeelBeat);
            Hide();
        }

        private void Update()
        {
            if (_ring == null)
            {
                return;
            }

            if (_ringHeld && Time.time - _buildAt > _settings.HomeLighting.BuildHoldTimeout)
            {
                StartHeld();
            }

            if (!_ring.Sample(Time.time, out float radius, out float strength))
            {
                if (_showing)
                {
                    Hide();
                }

                return;
            }

            ReachRingSettings tuning = _settings.ReachRing;
            Color colour = tuning.Color.linear * (tuning.Intensity * strength);
            Vector3 centre = _ring.Centre;
            Shader.SetGlobalVector(RingId, new Vector4(centre.x, centre.y, centre.z, radius));
            Shader.SetGlobalVector(RingColorId, new Vector4(colour.r, colour.g, colour.b, tuning.HalfWidth));
            _showing = true;
        }

        private void OnSignalRadius(SignalRadiusChanged change)
        {
            bool grew = _signalRadius >= 0f && change.Radius > _signalRadius;
            _signalRadius = change.Radius;
            if (!grew || !_context.TryGet(out IWorldLayout layout))
            {
                return;
            }

            if (_buildPending)
            {
                _ringHeld = true;
                _heldCentre = layout.BasePosition;
                _heldReach = change.Radius;
                return;
            }

            _ring.Start(layout.BasePosition, change.Radius, Time.time);
        }

        private void OnFeelBeat(FeelBeat beat)
        {
            if (beat.Kind != FeelKind.Build)
            {
                return;
            }

            if (beat.Phase == FeelPhase.Anticipate)
            {
                _buildPending = true;
                _buildAt = Time.time;
            }
            else if ((beat.Phase == FeelPhase.Follow && beat.Strength > RingFollowStrength)
                     || beat.Phase == FeelPhase.Cancel)
            {
                StartHeld();
            }
        }

        private void StartHeld()
        {
            _buildPending = false;
            if (_ringHeld)
            {
                _ringHeld = false;
                _ring.Start(_heldCentre, _heldReach, Time.time);
            }
        }

        private void OnRelayRestored(RelayRestored restored)
        {
            _ring.Start(restored.Position, _settings.ReachRing.RelayReach, Time.time);
        }

        private void OnBatteryChanged(BatteryChanged change)
        {
            bool fitted = !change.Restored && _cells >= 0 && change.Cells > _cells;
            _cells = change.Cells;
            if (fitted && _context.TryGet(out IRoverState rover))
            {
                _ring.Start(rover.Position, _settings.ReachRing.PackReach, Time.time);
            }
        }

        private void Hide()
        {
            Shader.SetGlobalVector(RingId, Vector4.zero);
            Shader.SetGlobalVector(RingColorId, Vector4.zero);
            _showing = false;
        }

        private void OnDestroy()
        {
            _signal?.Dispose();
            _relay?.Dispose();
            _battery?.Dispose();
            _feel?.Dispose();
            if (_ring != null)
            {
                Hide();
            }
        }
    }
}
