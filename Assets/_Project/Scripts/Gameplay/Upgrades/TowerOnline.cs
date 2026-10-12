using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The tower comes online (docs/design/feel-pass.md §8.2), as <see cref="FeelBeat"/>s of <see cref="FeelKind.Build"/>
    /// at the tower on every level-up, never on a load. Anticipate as the parts go into the hopper (Seconds = the
    /// tuned lead); then, from the stitch, four Acts as power climbs the tower ring by ring (strength 0.25 to 1), the
    /// Impact (the beacon), Follow 0 (power reaches home: cable, windows, radio), Follow 1 (the reach ring rolls out)
    /// and the Reward. <see cref="Step"/> says when power and the ring are due, so the tower shows them on their beat.
    /// </summary>
    public sealed class TowerOnline
    {
        /// <summary>Acts as power climbs the tower, one per ring of lamps.</summary>
        public const int Rings = 4;

        private readonly EventBus _events;
        private readonly RadioTowerTuning _tuning;
        private Vector3 _at;
        private float _start = float.NegativeInfinity;
        private float _elapsed;

        public TowerOnline(EventBus events, RadioTowerTuning tuning)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        /// <summary>True from the stitch until the Reward.</summary>
        public bool Running { get; private set; }

        /// <summary>How far power has climbed the tower, 0..1 (rings lit over <see cref="Rings"/>).</summary>
        public float Climb { get; private set; }

        /// <summary>The parts are going into the hopper: the sequence's Anticipate.</summary>
        public void Anticipate(Vector3 at)
        {
            _at = at;
            Publish(FeelPhase.Anticipate, 0f, _tuning.OnlineAnticipateSeconds);
        }

        /// <summary>The stitch starts: power begins to climb.</summary>
        public void Begin(Vector3 at, float now)
        {
            _at = at;
            _start = now;
            _elapsed = -1f;
            Climb = 0f;
            Running = true;
        }

        /// <summary>
        /// Publishes the beats due by <paramref name="now"/>. True in <paramref name="powered"/> on the Follow that
        /// brings power home, in <paramref name="ring"/> on the one that rolls the reach ring out.
        /// </summary>
        public void Step(float now, out bool powered, out bool ring)
        {
            powered = false;
            ring = false;
            if (!Running)
            {
                return;
            }

            float before = _elapsed;
            float t = now - _start;
            _elapsed = t;
            for (int i = 0; i < Rings; i++)
            {
                if (Crossed(before, t, i * _tuning.OnlineRingSeconds))
                {
                    Climb = (i + 1f) / Rings;
                    Publish(FeelPhase.Act, Climb, 0f);
                }
            }

            if (Crossed(before, t, _tuning.OnlineImpactAt))
            {
                Publish(FeelPhase.Impact, 1f, 0f);
            }

            if (Crossed(before, t, _tuning.OnlinePowerAt))
            {
                powered = true;
                Publish(FeelPhase.Follow, 0f, 0f);
            }

            if (Crossed(before, t, _tuning.OnlineRingAt))
            {
                ring = true;
                Publish(FeelPhase.Follow, 1f, 0f);
            }

            if (Crossed(before, t, _tuning.OnlineRewardAt))
            {
                Running = false;
                Publish(FeelPhase.Reward, 1f, 0f);
            }
        }

        /// <summary>True when <paramref name="mark"/> falls in (before, now].</summary>
        private static bool Crossed(float before, float now, float mark)
        {
            return before < mark && now >= mark;
        }

        private void Publish(FeelPhase phase, float strength, float seconds)
        {
            _events.Publish(new FeelBeat(FeelKind.Build, phase, _at, Vector3.up, strength, seconds));
        }
    }
}
