using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The charging dock in front of the lander (docs/features/M3-14, VISION ruling 12): where 07 rests at home. Its
    /// glow waits softly; once 07 has stood still on it with no drive input for a moment (<see cref="DockRest"/>) it
    /// announces the rest (<see cref="RoverDockChanged"/>: the rover settles 07 gently onto the dock's anchor and
    /// dims its lamp) and its glow warms and breathes while 07 charges. Any drive input ends the rest at once. Its lock
    /// (<see cref="DockLock"/>, feel-pass §7.1) raises four walls round 07 while it charges, lights their strips per
    /// cell, sweeps them to gold and lowers them when full, and chases the rim lamps as 07 rolls on.
    /// </summary>
    public sealed class ChargingDock
    {
        private readonly Transform _anchor;
        private readonly EmissionGlow _glow;
        private readonly BaseTuning _tuning;
        private readonly EventBus _events;
        private readonly DockRest _rest;
        private readonly DockLock _lock;
        private readonly Transform[] _walls;
        private readonly Vector3[] _wallRest;
        private readonly EmissionGlow[] _strips;
        private readonly float[] _stripLevels;
        private readonly EmissionGlow[] _rims;
        private IRoverBattery _battery;

        /// <param name="anchor">The lander's DockAnchor: 07's pivot at rest, facing the lander.</param>
        /// <param name="glow">The lander's DockGlow renderer.</param>
        /// <param name="walls">The lock's DockWall_0..3, standing raised as Art built them.</param>
        /// <param name="strips">Each wall's lamp strip renderer, in the same order.</param>
        /// <param name="rims">The rim lamp rings' renderers, outer to inner.</param>
        public ChargingDock(Transform anchor, Renderer glow, Transform[] walls, Renderer[] strips, Renderer[] rims,
            BaseTuning tuning, EventBus events)
        {
            if (walls == null || walls.Length != DockLock.Walls || strips == null || strips.Length != DockLock.Walls ||
                rims == null || rims.Length != DockLock.RimRings)
            {
                throw new ArgumentException($"The dock lock needs {DockLock.Walls} walls with their strips and " +
                                            $"{DockLock.RimRings} rim rings.");
            }

            _anchor = anchor != null ? anchor : throw new ArgumentNullException(nameof(anchor));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _glow = new EmissionGlow(glow);
            _rest = new DockRest(tuning.DockRadius, tuning.DockMaxSpeed, tuning.DockDelay, tuning.DockInputDeadZone);
            GlowLevel = tuning.DockIdleGlow;
            _glow.Apply(GlowLevel);
            _lock = new DockLock(events, tuning);
            _walls = walls;
            _wallRest = new Vector3[walls.Length];
            _strips = new EmissionGlow[strips.Length];
            _stripLevels = new float[strips.Length];
            for (int i = 0; i < walls.Length; i++)
            {
                _wallRest[i] = walls[i].localPosition;
                _strips[i] = new EmissionGlow(strips[i]);
            }

            _rims = new EmissionGlow[rims.Length];
            for (int i = 0; i < rims.Length; i++)
            {
                _rims[i] = new EmissionGlow(rims[i]);
            }

            ApplyLock(0f);
        }

        /// <summary>The lock (tests and debugging views).</summary>
        public DockLock Lock => _lock;

        /// <summary>The battery the dock charges: the lock follows its cells. Gameplay connects it once it is up.</summary>
        public void Connect(IRoverBattery battery)
        {
            _battery = battery ?? throw new ArgumentNullException(nameof(battery));
        }

        /// <summary>True while 07 rests on the dock.</summary>
        public bool Docked => _rest.Docked;

        /// <summary>Current glow of the dock (tests and debugging views).</summary>
        public float GlowLevel { get; private set; }

        /// <summary>Where 07 rests: the dock's anchor.</summary>
        public Vector3 Position => _anchor.position;

        /// <summary>Which way 07 faces at rest: towards the lander.</summary>
        public Quaternion Rotation => _anchor.rotation;

        public void Step(IRoverState rover, float now, float deltaTime)
        {
            float distance = SurfaceRules.HorizontalDistance(rover.Position, _anchor.position);
            if (_rest.Step(distance, rover.Speed, rover.DriveInput.magnitude, deltaTime))
            {
                _events.Publish(new RoverDockChanged(_rest.Docked, _anchor.position, _anchor.rotation));
            }

            float breath = 1f - _tuning.DockBreathDepth *
                (1f - MarkerEnvelope.Breath(now, _tuning.DockBreathPeriod));
            float target = _rest.Docked ? _tuning.DockChargingGlow * breath : _tuning.DockIdleGlow;
            GlowLevel = Damp.Toward(GlowLevel, target, _tuning.DockGlowEase, deltaTime);
            _glow.Apply(GlowLevel);
            if (_battery == null)
            {
                return;
            }

            _lock.Step(distance <= _tuning.DockRadius, _rest.Docked, _battery.Charge, _battery.Cells, _anchor.position,
                deltaTime);
            ApplyLock(deltaTime);
        }

        /// <summary>The walls at their rise, their strips lit and tinted, the rim lamps chasing.</summary>
        private void ApplyLock(float deltaTime)
        {
            Color tint = Color.Lerp(_tuning.StripCharging, _tuning.StripFull, _lock.Sweep);
            for (int i = 0; i < _walls.Length; i++)
            {
                float height = _lock.WallHeight(i);
                _walls[i].localPosition = _wallRest[i] -
                                          _walls[i].parent.InverseTransformVector(Vector3.up * _tuning.WallSink) *
                                          (1f - height);
                float target = height <= 0f ? 0f
                    : Mathf.Lerp(_tuning.StripUnlit, _tuning.StripLit, _lock.StripFill(i));
                _stripLevels[i] = Damp.Toward(_stripLevels[i], target, _tuning.StripEase, deltaTime);
                _strips[i].Apply(tint, _stripLevels[i]);
            }

            for (int ring = 0; ring < _rims.Length; ring++)
            {
                _rims[ring].Apply(_lock.RimLevel(ring) * _tuning.RimGlow);
            }
        }
    }
}
