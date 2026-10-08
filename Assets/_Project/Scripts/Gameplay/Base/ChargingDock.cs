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
    /// dims its lamp) and its glow warms and breathes while 07 charges. Any drive input ends the rest at once.
    /// </summary>
    public sealed class ChargingDock
    {
        private readonly Transform _anchor;
        private readonly EmissionGlow _glow;
        private readonly BaseTuning _tuning;
        private readonly EventBus _events;
        private readonly DockRest _rest;

        /// <param name="anchor">The lander's DockAnchor: 07's pivot at rest, facing the lander.</param>
        /// <param name="glow">The lander's DockGlow renderer.</param>
        public ChargingDock(Transform anchor, Renderer glow, BaseTuning tuning, EventBus events)
        {
            _anchor = anchor != null ? anchor : throw new ArgumentNullException(nameof(anchor));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _glow = new EmissionGlow(glow);
            _rest = new DockRest(tuning.DockRadius, tuning.DockMaxSpeed, tuning.DockDelay, tuning.DockInputDeadZone);
            GlowLevel = tuning.DockIdleGlow;
            _glow.Apply(GlowLevel);
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
        }
    }
}
