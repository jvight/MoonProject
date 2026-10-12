using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The cutter grows (docs/features/M3-26): its tier follows the fitted kits (a load restores it without an
    /// install moment), and a big piece cuts in segments, the beam stopping at each segment's end to slide on.
    /// </summary>
    public sealed class CutterTests
    {
        [Test]
        public void TheTier_FollowsTheFocusLens_AndSaysWhenALoadRestoredIt()
        {
            var events = new EventBus();
            var changes = new List<CutterTierChanged>();
            events.Subscribe<CutterTierChanged>(changes.Add);
            var tuning = ScriptableObject.CreateInstance<CutterTuning>();
            try
            {
                var abilities = new Abilities();
                abilities.Grant(RoverAbility.FocusLens);
                var restored = new Cutter(events, abilities, tuning);
                restored.Step();
                Assert.AreEqual(1, restored.Tier);
                Assert.IsTrue(changes[0].Restored, "a load shows the lens at once");

                var fresh = new Cutter(events, new Abilities(), tuning);
                fresh.Step();
                Assert.AreEqual(0, fresh.Tier);
                Assert.AreEqual(0.65f, fresh.ChargeSeconds, 1e-5f, "M3-26 §3: the old cutter");
                Assert.AreEqual(5, fresh.LampCount);
                Assert.AreEqual(1, changes.Count, "tier 0 says nothing");

                var later = new Abilities();
                var cutter = new Cutter(events, later, tuning);
                cutter.Step();
                later.Grant(RoverAbility.FocusLens);
                cutter.Step();
                Assert.AreEqual(1, cutter.Tier);
                Assert.IsFalse(changes[changes.Count - 1].Restored, "fitted now: the install moment plays");
                Assert.AreEqual(0.45f, cutter.ChargeSeconds, 1e-5f);
                Assert.AreEqual(0.2f, cutter.ResumeChargeSeconds, 1e-5f);
                Assert.AreEqual(1.3f, cutter.SpeedFactor, 1e-5f);
                Assert.AreEqual(0.85f, cutter.DrainFactor, 1e-5f);
                Assert.AreEqual(7, cutter.LampCount);
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }

        [Test]
        public void ABigPiece_StopsAtEachSegmentsEnd_ThenGoesOn()
        {
            float progress = 0.6f;
            Assert.IsTrue(CutSegments.ReachedSegmentEnd(0.4f, ref progress, 2), "past the first half");
            Assert.AreEqual(0.5f, progress, 1e-6f, "held at the segment's end for the slide");

            float next = 0.55f;
            Assert.IsFalse(CutSegments.ReachedSegmentEnd(progress, ref next, 2), "from the end, the next segment");
            Assert.AreEqual(0.55f, next, 1e-6f);

            float last = 1f;
            Assert.IsFalse(CutSegments.ReachedSegmentEnd(0.9f, ref last, 2), "the last segment ends in the break");
            float small = 0.7f;
            Assert.IsFalse(CutSegments.ReachedSegmentEnd(0.2f, ref small, 1), "a one-segment piece never stops");

            float hull = 0.34f;
            Assert.IsTrue(CutSegments.ReachedSegmentEnd(0.3f, ref hull, 3));
            Assert.AreEqual(1f / 3f, hull, 1e-6f);
        }

        private sealed class Abilities : IRoverAbilities
        {
            private readonly HashSet<RoverAbility> _granted = new HashSet<RoverAbility>();

            public bool Has(RoverAbility ability)
            {
                return _granted.Contains(ability);
            }

            public void Grant(RoverAbility ability)
            {
                _granted.Add(ability);
            }
        }
    }
}
