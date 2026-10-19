using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The base's lamps follow the Sun: as it drops through dusk (to <see cref="BaseTuning.LampDuskAboveLastLightDegrees"/>
    /// above last light, or while the opening eclipse holds) the built pieces' lamps come on one by one in the
    /// base's order, each easing up like an old lamp warming (<see cref="LampEase"/>) and raising
    /// <see cref="BaseLampLit"/>; once the Sun climbs back they ease off together. A piece built at dusk lights at once;
    /// one restored from a save starts as the sky says. Allocation-free per frame.
    /// </summary>
    public sealed class BaseLamps : IDisposable
    {
        private readonly BaseTuning _tuning;
        private readonly BuildField _building;
        private readonly ISunlight _sunlight;
        private readonly EventBus _events;
        private readonly List<Lamp> _lamps = new List<Lamp>();
        private readonly Comparison<Lamp> _byRank;
        private readonly IDisposable _builtSubscription;
        private readonly IDisposable _removedSubscription;
        private bool _started;
        private bool _dusk;

        public BaseLamps(EventBus events, BaseTuning tuning, BuildField building, ISunlight sunlight)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _building = building != null ? building : throw new ArgumentNullException(nameof(building));
            _sunlight = sunlight ?? throw new ArgumentNullException(nameof(sunlight));
            _byRank = CompareRank;
            _builtSubscription = events.Subscribe<StructureBuilt>(OnBuilt);
            _removedSubscription = events.Subscribe<StructureRemoved>(OnRemoved);
        }

        /// <summary>It is dusk at the base: the lamps are lit or coming on.</summary>
        public bool Dusk => _dusk;

        /// <summary>How many built pieces carry a lamp.</summary>
        public int Count => _lamps.Count;

        /// <summary>The glow (0..1) of the lamp of structure <paramref name="instanceId"/>, or -1 when it has none.
        /// </summary>
        public float LevelOf(int instanceId)
        {
            for (int i = 0; i < _lamps.Count; i++)
            {
                if (_lamps[i].InstanceId == instanceId)
                {
                    return _lamps[i].Ease.Level;
                }
            }

            return -1f;
        }

        public void Dispose()
        {
            _builtSubscription.Dispose();
            _removedSubscription.Dispose();
        }

        public void Step(float deltaTime)
        {
            bool dusk = Dusky();
            if (!_started)
            {
                // The game is loaded: each lamp starts as the sky says, with no ease and no beat.
                _started = true;
                _dusk = dusk;
                for (int i = 0; i < _lamps.Count; i++)
                {
                    _lamps[i].Ease.Snap(dusk);
                }
            }
            else if (dusk != _dusk)
            {
                _dusk = dusk;
                for (int i = 0; i < _lamps.Count; i++)
                {
                    if (dusk)
                    {
                        _lamps[i].Ease.Light(i * _tuning.LampStaggerSeconds);
                    }
                    else
                    {
                        _lamps[i].Ease.Extinguish();
                    }
                }
            }

            for (int i = 0; i < _lamps.Count; i++)
            {
                Lamp lamp = _lamps[i];
                if (lamp.Ease.Step(deltaTime, _tuning.LampRiseSeconds, _tuning.LampFallSeconds,
                        _tuning.LampFlickerDepth, _tuning.LampFlickerHertz, _tuning.LampFlickerSeconds))
                {
                    _events.Publish(new BaseLampLit(lamp.InstanceId, i));
                }

                if (_building.TryGetView(lamp.InstanceId, out StructureView view))
                {
                    view.DriveLamps(lamp.Ease.Level);
                }
            }
        }

        /// <summary>Dusk with a margin the other way, so a Sun at the threshold does not flicker the lamps.</summary>
        private bool Dusky()
        {
            if (_sunlight.IsHoldingEclipse)
            {
                return true;
            }

            float elevation = Mathf.Asin(Mathf.Clamp(_sunlight.SunDirection.y, -1f, 1f)) * Mathf.Rad2Deg;
            float threshold = _sunlight.LastLightElevationDegrees + _tuning.LampDuskAboveLastLightDegrees;
            return _dusk ? elevation <= threshold + _tuning.LampDawnMarginDegrees : elevation <= threshold;
        }

        private void OnBuilt(StructureBuilt built)
        {
            for (int i = 0; i < _lamps.Count; i++)
            {
                if (_lamps[i].InstanceId == built.InstanceId)
                {
                    return;
                }
            }

            var lamp = new Lamp(built.InstanceId, Rank(built.StructureId));
            if (_started && !built.Restored)
            {
                lamp.Ease.Snap(false);
                if (_dusk)
                {
                    lamp.Ease.Light(0f);
                }
            }

            _lamps.Add(lamp);
            _lamps.Sort(_byRank);
        }

        private void OnRemoved(StructureRemoved removed)
        {
            for (int i = 0; i < _lamps.Count; i++)
            {
                if (_lamps[i].InstanceId == removed.InstanceId)
                {
                    _lamps.RemoveAt(i);
                    return;
                }
            }
        }

        private int Rank(string structureId)
        {
            IReadOnlyList<string> order = _tuning.LampOrder;
            for (int i = 0; i < order.Count; i++)
            {
                if (string.Equals(order[i], structureId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return order.Count;
        }

        private static int CompareRank(Lamp a, Lamp b)
        {
            int byRank = a.Rank.CompareTo(b.Rank);
            return byRank != 0 ? byRank : a.InstanceId.CompareTo(b.InstanceId);
        }

        private sealed class Lamp
        {
            public Lamp(int instanceId, int rank)
            {
                InstanceId = instanceId;
                Rank = rank;
            }

            public int InstanceId { get; }

            public int Rank { get; }

            public LampEase Ease { get; } = new LampEase();
        }
    }
}
