using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The tower comes online (docs/design/feel-pass.md §8.2): Anticipate at the hopper, then from the stitch four
    /// Acts climbing 0.25 to 1, the Impact, Follow 0 (power home), Follow 1 (the reach ring) and the Reward, in order
    /// and once each; and a tower step that names its exact sub-step.
    /// </summary>
    public sealed class TowerOnlineTests
    {
        private const float Frame = 1f / 60f;

        [Test]
        public void TheSequence_PublishesItsBeatsInOrder_AndSaysWhenPowerAndTheRingAreDue()
        {
            var events = new EventBus();
            var beats = new List<FeelBeat>();
            events.Subscribe<FeelBeat>(beats.Add);
            var tuning = ScriptableObject.CreateInstance<RadioTowerTuning>();
            try
            {
                var online = new TowerOnline(events, tuning);
                online.Anticipate(Vector3.zero);
                Assert.AreEqual(FeelPhase.Anticipate, beats[0].Phase);
                Assert.AreEqual(FeelKind.Build, beats[0].Kind);
                Assert.AreEqual(tuning.OnlineAnticipateSeconds, beats[0].Seconds, 1e-5f);

                online.Begin(Vector3.up, 10f);
                float poweredAt = -1f;
                float ringAt = -1f;
                for (float t = 10f; online.Running; t += Frame)
                {
                    online.Step(t, out bool powered, out bool ring);
                    poweredAt = powered ? t - 10f : poweredAt;
                    ringAt = ring ? t - 10f : ringAt;
                }

                var phases = new List<FeelPhase>();
                var strengths = new List<float>();
                foreach (FeelBeat beat in beats)
                {
                    phases.Add(beat.Phase);
                    strengths.Add(beat.Strength);
                }

                CollectionAssert.AreEqual(new[]
                {
                    FeelPhase.Anticipate, FeelPhase.Act, FeelPhase.Act, FeelPhase.Act, FeelPhase.Act,
                    FeelPhase.Impact, FeelPhase.Follow, FeelPhase.Follow, FeelPhase.Reward,
                }, phases);
                CollectionAssert.AreEqual(new[] { 0.25f, 0.5f, 0.75f, 1f }, strengths.GetRange(1, 4));
                Assert.AreEqual(0f, strengths[6], "Follow 0: power reaches home");
                Assert.AreEqual(1f, strengths[7], "Follow 1: the reach ring");
                Assert.AreEqual(tuning.OnlinePowerAt, poweredAt, 2f * Frame);
                Assert.AreEqual(tuning.OnlineRingAt, ringAt, 2f * Frame);
                Assert.LessOrEqual(tuning.OnlineAnticipateSeconds + tuning.OnlineRewardAt, 6.5f, "about 6 s in all");
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }

        [Test]
        public void AStep_CanNameItsExactSubStep()
        {
            var events = new EventBus();
            var lines = new List<string>();
            events.Subscribe<NextStepChanged>(changed => lines.Add(changed.LineKey));
            var flow = new StepFlow(events);
            string now = ProgressionKeys.NextTowerPart;
            bool done = false;
            flow.Add(() => now, () => done);
            flow.Tick();
            now = ProgressionKeys.NextTowerPad;
            flow.Tick();
            now = ProgressionKeys.NextTowerHold;
            flow.Tick();
            done = true;
            flow.Tick();
            CollectionAssert.AreEqual(new[]
            {
                ProgressionKeys.NextTowerPart, ProgressionKeys.NextTowerPad, ProgressionKeys.NextTowerHold, string.Empty,
            }, lines);
        }
    }
}
