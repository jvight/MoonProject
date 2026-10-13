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
                Assert.AreEqual(1.07f, fresh.ChargeSeconds, 1e-5f, "0.5.5 §5.1: brace 0.5 + one stage 0.45 + breath");
                Assert.AreEqual(0.5f, fresh.BraceSeconds, 1e-5f);
                Assert.AreEqual(6f, fresh.WarmWindowSeconds, 1e-5f);
                Assert.AreEqual(5, fresh.LampCount);
                Assert.AreEqual(1, changes.Count, "tier 0 says nothing");

                var later = new Abilities();
                var cutter = new Cutter(events, later, tuning);
                cutter.Step();
                later.Grant(RoverAbility.FocusLens);
                cutter.Step();
                Assert.AreEqual(1, cutter.Tier);
                Assert.IsFalse(changes[changes.Count - 1].Restored, "fitted now: the install moment plays");
                Assert.AreEqual(0.98f, cutter.ChargeSeconds, 1e-5f, "tiers shorten the stages, never the brace");
                Assert.AreEqual(0.36f * 0.6f + 0.12f, cutter.ResumeChargeSeconds, 1e-5f, "warm: no brace");
                ChargePlan hull = cutter.Plan(3, false);
                Assert.AreEqual(0.5f + 3f * 0.36f + 0.12f, hull.TotalSeconds, 1e-5f);
                ChargePlan warm = cutter.Plan(3, true);
                Assert.AreEqual(0f, warm.BraceSeconds);
                Assert.Greater(warm.StartFraction, 0f, "the warm run starts part-way");
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

            public void Revoke(RoverAbility ability)
            {
                _granted.Remove(ability);
            }
        }
    }
}
