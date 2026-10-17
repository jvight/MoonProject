using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07's cutter as its fitted kits make it (docs/features/M3-26, <see cref="ICutter"/>): the highest tier whose kit
    /// is fitted. A change publishes <see cref="CutterTierChanged"/>; the first one after a boot counts as restored (a
    /// load shows the tier at once, with no install moment). It cuts only while 07's start kit says it works
    /// (opening-v2 §10: FAULT until the igniter is fitted at B6); a press it refuses publishes
    /// <see cref="CutterRefused"/> once. Allocation-free.
    /// </summary>
    public sealed class Cutter : ICutter
    {
        private readonly EventBus _events;
        private readonly IRoverAbilities _abilities;
        private readonly CutterTuning _tuning;
        private IRoverSystems _systems;
        private bool _stepped;
        private bool _refusing;
        private bool _refusedThisFrame;

        public Cutter(EventBus events, IRoverAbilities abilities, CutterTuning tuning)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _abilities = abilities ?? throw new ArgumentNullException(nameof(abilities));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        public int Tier { get; private set; }

        /// <summary>The cutter works: its igniter is fitted (opening-v2 §10).</summary>
        public bool Ready => _systems.StateOf(RoverSystem.Cutter) == RoverSystemState.Ok;

        /// <summary>07's systems: the cutter cuts only while they say it works.</summary>
        internal void ConnectSystems(IRoverSystems systems)
        {
            _systems = systems ?? throw new ArgumentNullException(nameof(systems));
        }

        /// <summary>
        /// A held E asked the cutter, not <see cref="Ready"/>, to cut at <paramref name="point"/> this frame: the first
        /// frame of such a press publishes <see cref="CutterRefused"/>.
        /// </summary>
        public void Refuse(Vector3 point)
        {
            if (!_refusing)
            {
                _events.Publish(new CutterRefused(point));
            }

            _refusing = true;
            _refusedThisFrame = true;
        }

        public int LampCount => _tuning.Tier(Tier).LampCount;

        /// <summary>A small piece's fresh charge: the brace, one stage and the breath.</summary>
        public float ChargeSeconds => _tuning.BraceSeconds + StageSeconds + _tuning.BreathSeconds;

        /// <summary>A small piece's warm charge: one quicker stage and the breath.</summary>
        public float ResumeChargeSeconds => StageSeconds * _tuning.WarmFactor + _tuning.BreathSeconds;

        public float BraceSeconds => _tuning.BraceSeconds;

        public float WarmWindowSeconds => _tuning.WarmWindowSeconds;

        /// <summary>Seconds one charge stage takes at this tier (fresh).</summary>
        public float StageSeconds => _tuning.Tier(Tier).StageSeconds;

        /// <summary>
        /// The charge plan for a piece needing <paramref name="stages"/> stages, warm or fresh (0.5.5 §5.1).
        /// </summary>
        public ChargePlan Plan(int stages, bool warm)
        {
            float stage = warm ? StageSeconds * _tuning.WarmFactor : StageSeconds;
            float fresh = _tuning.BraceSeconds + stages * StageSeconds + _tuning.BreathSeconds;
            float total = stages * stage + _tuning.BreathSeconds;
            float start = warm && fresh > 0f ? Mathf.Clamp01(1f - total / fresh) : 0f;
            return new ChargePlan(_tuning.BraceSeconds, stage, stages, _tuning.BreathSeconds, warm, start);
        }

        public float SpeedFactor => _tuning.Tier(Tier).SpeedFactor;

        public float DrainFactor => _tuning.Tier(Tier).DrainFactor;

        /// <summary>Follows the fitted kits and ends a refused press once E stops asking; call each frame.</summary>
        public void Step()
        {
            _refusing = _refusedThisFrame;
            _refusedThisFrame = false;
            int tier = 0;
            for (int i = 1; i < _tuning.TierCount; i++)
            {
                if (_abilities.Has(_tuning.KitFor(i)))
                {
                    tier = i;
                }
            }

            bool restored = !_stepped;
            _stepped = true;
            if (tier == Tier)
            {
                return;
            }

            Tier = tier;
            _events.Publish(new CutterTierChanged(tier, restored));
        }
    }
}
