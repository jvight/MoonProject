using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The antenna mast's raise (docs/design/opening-v3-plan.md 3.3): once the mast is built it rises in segments,
    /// base, middle, top, each lifting for <see cref="CallReplySettings.SegmentLiftSeconds"/> (eased) and locking with
    /// <see cref="MastSegmentRaised"/>, the last settling before the raise is done. A mast built in an earlier session
    /// stands raised: it is done at once and publishes nothing. EditMode-tested; allocation-free per frame.
    /// </summary>
    public sealed class MastRaise : IDisposable
    {
        private readonly EventBus _events;
        private readonly CallReplySettings _settings;
        private readonly IDisposable _built;
        private float _seconds;
        private int _locked;
        private Vector3 _position;
        private bool _active;

        public MastRaise(EventBus events, CallReplySettings settings)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _built = events.Subscribe<StructureBuilt>(OnBuilt);
        }

        /// <summary>Segments in the raise.</summary>
        public int Segments => _settings.Segments;

        /// <summary>The raise is running now.</summary>
        public bool Active => _active;

        /// <summary>The mast stands built and fully raised (or no mast has been built yet: false).</summary>
        public bool Done { get; private set; }

        /// <summary>How far segment <paramref name="index"/> has lifted now, 0 (lowered) to 1 (locked home).</summary>
        public float Lift01(int index)
        {
            if (index < 0 || index >= _settings.Segments)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "No such mast segment.");
            }

            if (Done)
            {
                return 1f;
            }

            return _active ? Lift(_settings, index, _seconds) : 0f;
        }

        public void Step(float deltaTime)
        {
            if (!_active)
            {
                return;
            }

            _seconds += deltaTime;
            while (_locked < _settings.Segments && _seconds >= LockTime(_settings, _locked))
            {
                _events.Publish(new MastSegmentRaised(_locked, _settings.Segments, _position, false));
                _locked++;
            }

            if (_seconds >= TotalSeconds(_settings))
            {
                _active = false;
                Done = true;
            }
        }

        public void Dispose()
        {
            _built.Dispose();
        }

        /// <summary>Seconds after the build when segment <paramref name="index"/> locks home.</summary>
        public static float LockTime(CallReplySettings settings, int index)
        {
            return (index + 1) * settings.SegmentLiftSeconds + index * settings.SegmentGapSeconds;
        }

        /// <summary>Seconds the whole raise takes: every segment, then the settle.</summary>
        public static float TotalSeconds(CallReplySettings settings)
        {
            return LockTime(settings, settings.Segments - 1) + settings.SettleSeconds;
        }

        /// <summary>Segment <paramref name="index"/>'s lift 0..1 at <paramref name="seconds"/> after the build.</summary>
        public static float Lift(CallReplySettings settings, int index, float seconds)
        {
            float start = LockTime(settings, index) - settings.SegmentLiftSeconds;
            return Smoothing01(Mathf.Clamp01((seconds - start) / settings.SegmentLiftSeconds));
        }

        private static float Smoothing01(float t)
        {
            return t * t * (3f - 2f * t);
        }

        private void OnBuilt(StructureBuilt built)
        {
            if (!string.Equals(built.StructureId, StructureIds.AntennaMast, StringComparison.Ordinal))
            {
                return;
            }

            _position = built.Position;
            if (built.Restored)
            {
                _active = false;
                Done = true;
                return;
            }

            _seconds = 0f;
            _locked = 0;
            Done = false;
            _active = true;
        }
    }
}
